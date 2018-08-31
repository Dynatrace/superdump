using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Options;
using SuperDumpService.Models;

namespace SuperDumpService.Services {
	public class JiraIssueRepository {
		private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

		private readonly JiraApiService apiService;
		private readonly BundleRepository bundleRepo;
		private readonly JiraIssueStorageFilebased jiraIssueStorage;
		private readonly SuperDumpSettings settings;
		private readonly ConcurrentDictionary<string, IEnumerable<JiraIssueModel>> bundleIssues = new ConcurrentDictionary<string, IEnumerable<JiraIssueModel>>();


		public JiraIssueRepository(IOptions<SuperDumpSettings> settings, JiraApiService apiService, BundleRepository bundleRepo, JiraIssueStorageFilebased jiraIssueStorage) {
			this.apiService = apiService;
			this.bundleRepo = bundleRepo;
			this.jiraIssueStorage = jiraIssueStorage;
			this.settings = settings.Value;
		}

		public async Task Populate() {
			await semaphoreSlim.WaitAsync().ConfigureAwait(false);
			try {
				foreach (BundleMetainfo bundle in bundleRepo.GetAll()) {
					try {
						IEnumerable<JiraIssueModel> jiraIssues = await jiraIssueStorage.Read(bundle.BundleId);
						if (jiraIssues != null) {
							bundleIssues[bundle.BundleId] = jiraIssues;
						}
					} catch (Exception e) {
						Console.WriteLine("error reading jira-issue file: " + e.ToString());
						jiraIssueStorage.Wipe(bundle.BundleId);
					}
				}
			} finally {
				semaphoreSlim.Release();
			}
		}

		public async Task<IEnumerable<JiraIssueModel>> GetIssuesByBundle(string bundleId) {
			await semaphoreSlim.WaitAsync().ConfigureAwait(false);
			try {
				return bundleIssues.GetValueOrDefault(bundleId);
			} finally {
				semaphoreSlim.Release();
			}
		}

		public IDictionary<string, IEnumerable<JiraIssueModel>> GetIssuesByBundleIdsWithoutWait(IEnumerable<string> bundleIds) {
			return bundleIds.ToDictionary(bundleId => bundleId, bundleId => bundleIssues.GetValueOrDefault(bundleId));
		}

		public async Task WipeJiraIssueCache() {
			await semaphoreSlim.WaitAsync().ConfigureAwait(false);
			try {
				foreach (KeyValuePair<string, IEnumerable<JiraIssueModel>> item in bundleIssues) {
					jiraIssueStorage.Wipe(item.Key);
				}
				bundleIssues.Clear();
			} finally {
				semaphoreSlim.Release();
			}
		}

		public void StartRefreshHangfireJob() {
			RecurringJob.AddOrUpdate(() => RefreshAllIssuesAsync(false), settings.JiraIssueRefreshCron, null, "jirastatus");
		}

		[Queue("jirastatus")]
		public async Task RefreshAllIssuesAsync(bool force = false) {
			await semaphoreSlim.WaitAsync().ConfigureAwait(false);
			try {
				ILookup<bool, JiraIssueModel> issuesToRefresh = bundleIssues.SelectMany(issue => issue.Value).
					ToLookup(issue => force || issue.StatusName != "Resolved");
				if (issuesToRefresh[true].Any()) {
					var refreshedIssues = issuesToRefresh[false]
						.Union(await apiService.GetBulkIssues(issuesToRefresh[true].Select(i => i.Key)))
						.ToDictionary(issue => issue.Key, issue => issue);

					foreach (string bundleId in bundleIssues.Keys) {
						IEnumerable<JiraIssueModel> issues = bundleIssues[bundleId].Select(issue => refreshedIssues.GetValueOrDefault(issue.Key));
						await jiraIssueStorage.Store(bundleId, bundleIssues[bundleId] = issues);
					}
				}
			} finally {
				semaphoreSlim.Release();
			}
		}

		public void StartBundleSearchHangfireJob() {
			RecurringJob.AddOrUpdate(() => SearchAllBundleIssues(false), settings.JiraBundleIssueSearchCron, null, "jirastatus");
		}

		[Queue("jirastatus")]
		public void SearchAllBundleIssues(bool force = false) {
			IEnumerable<BundleMetainfo> bundles = bundleRepo.GetAll().Where(bundle => DateTime.Now - bundle.Created <= settings.JiraBundleSearchTimeSpan);
			int idx = 0;

			while (bundles.Skip(idx).Any()) {
				BackgroundJob.Schedule(() => SearchBundleIssuesAsync(bundles.Skip(idx).Take(settings.JiraBundleSearchLimit), false),
					TimeSpan.FromMinutes(settings.JiraBundleSearchDelay * idx / settings.JiraBundleSearchLimit));
				idx += settings.JiraBundleSearchLimit;
			}
		}

		[Queue("jirastatus")]
		public async Task SearchBundleIssuesAsync(IEnumerable<BundleMetainfo> bundles, bool force = false) {
			await semaphoreSlim.WaitAsync().ConfigureAwait(false);
			try {
				await Task.WhenAll(bundles
					.Where(bundle => force || !bundleIssues.TryGetValue(bundle.BundleId, out IEnumerable<JiraIssueModel> issues) || !issues.Any()) //All bundles where no jira issue is known
					.Select(bundle => SearchBundleAsync(bundle, force)));
			} finally {
				semaphoreSlim.Release();
			}
		}

		private async Task SearchBundleAsync(BundleMetainfo bundle, bool force) {
			IEnumerable<JiraIssueModel> jiraIssues;
			if (!force && bundle.CustomProperties.TryGetValue(settings.CustomPropertyJiraIssueKey, out string jiraIssue)) {
				jiraIssues = new List<JiraIssueModel>() { new JiraIssueModel { Key = jiraIssue } };
			} else {
				jiraIssues = await apiService.GetJiraIssues(bundle.BundleId);
			}
			await jiraIssueStorage.Store(bundle.BundleId, bundleIssues[bundle.BundleId] = jiraIssues);
		}
	}
}
