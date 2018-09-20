using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SuperDumpService.Models;

namespace SuperDumpService.Services {
	public class JiraIssueRepository {
		private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

		private readonly JiraApiService apiService;
		private readonly BundleRepository bundleRepo;
		private readonly JiraIssueStorageFilebased jiraIssueStorage;
		private readonly IdenticalDumpRepository identicalDumpRepository;
		private readonly JiraIntegrationSettings settings;
		private readonly ILogger<JiraIssueRepository> logger;
		private readonly ConcurrentDictionary<string, IEnumerable<JiraIssueModel>> bundleIssues = new ConcurrentDictionary<string, IEnumerable<JiraIssueModel>>();


		public JiraIssueRepository(IOptions<SuperDumpSettings> settings,
				JiraApiService apiService,
				BundleRepository bundleRepo,
				JiraIssueStorageFilebased jiraIssueStorage,
				IdenticalDumpRepository identicalDumpRepository,
				ILoggerFactory loggerFactory) {
			this.apiService = apiService;
			this.bundleRepo = bundleRepo;
			this.jiraIssueStorage = jiraIssueStorage;
			this.identicalDumpRepository = identicalDumpRepository;
			this.settings = settings.Value.JiraIntegrationSettings;
			logger = loggerFactory.CreateLogger<JiraIssueRepository>();
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
						logger.LogError("error reading jira-issue file: " + e.ToString());
						jiraIssueStorage.Wipe(bundle.BundleId);
					}
				}
			} finally {
				semaphoreSlim.Release();
			}
		}

		public IEnumerable<JiraIssueModel> GetIssues(string bundleId) {
			return bundleIssues.GetValueOrDefault(bundleId, Enumerable.Empty<JiraIssueModel>());
		}

		public async Task<IEnumerable<JiraIssueModel>> GetAllIssuesByBundleIdWithoutWait(string bundleId) {
				return GetIssues(bundleId).Concat((await identicalDumpRepository.GetIdenticalRelationships(bundleId))
							.SelectMany(identicalBundle => GetIssues(identicalBundle)));
		}

		public async Task<IDictionary<string, IEnumerable<JiraIssueModel>>> GetAllIssuesByBundleIdsWithoutWait(IEnumerable<string> bundleIds) {
			var result = new Dictionary<string, IEnumerable<JiraIssueModel>>();
			foreach (string bundleId in bundleIds.Distinct()) {
				IEnumerable<JiraIssueModel> issues = await GetAllIssuesByBundleIdWithoutWait(bundleId);
				if (issues.Any()) {
					result.Add(bundleId, issues);
				}
			}

			return result;
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
			RecurringJob.AddOrUpdate(() => RefreshAllIssuesAsync(), settings.JiraIssueRefreshCron, null, "jirastatus");
		}

		[Queue("jirastatus")]
		public async Task RefreshAllIssuesAsync() {
			await semaphoreSlim.WaitAsync().ConfigureAwait(false);
			try {
				//Only update bundles with unresolved issues
				IEnumerable<KeyValuePair<string, IEnumerable<JiraIssueModel>>> bundlesToRefresh =
					bundleIssues.Where(bundle => bundle.Value.Any(issue => issue.GetStatusName() != "Resolved"));

				if (!bundlesToRefresh.Any()) {
					return;
				}

				//Split issues into one group with all resolved issues and one with all others
				ILookup<bool, JiraIssueModel> issuesToRefresh = bundlesToRefresh.SelectMany(issue => issue.Value).
					ToLookup(issue => issue.GetStatusName() != "Resolved");

				//Get the current status of not resolved issues from the jira api and combine them with the resolved issues
				IEnumerable<JiraIssueModel> refreshedIssues = issuesToRefresh[false]
						.Union(await apiService.GetBulkIssues(issuesToRefresh[true].Select(i => i.Key)));

				await SetBundleIssues(bundlesToRefresh, refreshedIssues);
			} finally {
				semaphoreSlim.Release();
			}
		}

		[Queue("jirastatus")]
		public async Task ForceRefreshAllIssuesAsync() {
			await semaphoreSlim.WaitAsync().ConfigureAwait(false);
			try {
				if (!bundleIssues.Any()) {
					return;
				}

				//Get the status of each issue 
				IEnumerable<JiraIssueModel> refreshedIssues =
					await apiService.GetBulkIssues(bundleIssues.SelectMany(issue => issue.Value).Select(issue => issue.Key));

				await SetBundleIssues(bundleIssues, refreshedIssues);
			} finally {
				semaphoreSlim.Release();
			}
		}

		public void StartBundleSearchHangfireJob() {
			RecurringJob.AddOrUpdate(() => SearchAllBundleIssues(settings.JiraBundleSearchTimeSpan, false), settings.JiraBundleIssueSearchCron, null, "jirastatus");
		}

		[Queue("jirastatus")]
		public void SearchAllBundleIssues(TimeSpan searchTimeSpan, bool force = false) {
			IEnumerable<BundleMetainfo> bundles = bundleRepo.GetAll().Where(bundle => DateTime.Now - bundle.Created <= searchTimeSpan);
			int idx = 0;

			while (bundles.Skip(idx).Any()) {
				BackgroundJob.Schedule(() => SearchBundleIssuesAsync(bundles.Skip(idx).Take(settings.JiraBundleSearchLimit), force),
					TimeSpan.FromMinutes(settings.JiraBundleSearchDelay * idx / settings.JiraBundleSearchLimit));
				idx += settings.JiraBundleSearchLimit;
			}
		}

		[Queue("jirastatus")]
		public async Task SearchBundleIssuesAsync(IEnumerable<BundleMetainfo> bundles, bool force = false) {
			await semaphoreSlim.WaitAsync().ConfigureAwait(false);
			try {
				IEnumerable<BundleMetainfo> bundlesToSearch = force ? bundles :
					bundles.Where(bundle => !bundleIssues.TryGetValue(bundle.BundleId, out IEnumerable<JiraIssueModel> issues) || !issues.Any()); //All bundles without issues

				await Task.WhenAll(bundlesToSearch.Select(bundle => SearchBundleAsync(bundle, force)));
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
			if (jiraIssues.Any()) {
				await jiraIssueStorage.Store(bundle.BundleId, bundleIssues[bundle.BundleId] = jiraIssues);
			}
		}

		private async Task SetBundleIssues(IEnumerable<KeyValuePair<string, IEnumerable<JiraIssueModel>>> bundlesToUpdate, IEnumerable<JiraIssueModel> refreshedIssues) {
			var issueDictionary = refreshedIssues.ToDictionary(issue => issue.Key, issue => issue);

			//Select the issues for each bundle and store them in the bundleIssues Dictionary
			//I am not sure if this is the best way to do this
			var fileStorageTasks = new List<Task>();
			foreach (KeyValuePair<string, IEnumerable<JiraIssueModel>> bundle in bundlesToUpdate) {
				IEnumerable<JiraIssueModel> issues = bundle.Value.Select(issue => issueDictionary[issue.Key]);
				fileStorageTasks.Add(jiraIssueStorage.Store(bundle.Key, bundleIssues[bundle.Key] = issues)); //update the issue file for the bundle
			}

			await Task.WhenAll(fileStorageTasks);
		}
	}
}
