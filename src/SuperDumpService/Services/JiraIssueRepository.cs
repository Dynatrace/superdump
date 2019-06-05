using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SuperDumpService.Helpers;
using SuperDumpService.Models;

namespace SuperDumpService.Services {
	public class JiraIssueRepository {
		private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

		private readonly IJiraApiService apiService;
		private readonly BundleRepository bundleRepo;
		private readonly IJiraIssueStorage jiraIssueStorage;
		private readonly IdenticalDumpRepository identicalDumpRepository;
		private readonly JiraIntegrationSettings settings;
		private readonly ILogger<JiraIssueRepository> logger;
		private readonly ConcurrentDictionary<string, IList<JiraIssueModel>> bundleIssues = new ConcurrentDictionary<string, IList<JiraIssueModel>>();
		public bool IsPopulated { get; private set; } = false;

		public JiraIssueRepository(IOptions<SuperDumpSettings> settings,
				IJiraApiService apiService,
				BundleRepository bundleRepo,
				IJiraIssueStorage jiraIssueStorage,
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
			await BlockIfBundleRepoNotReady("JiraIssueRepository.Populate");

			await semaphoreSlim.WaitAsync().ConfigureAwait(false);
			try {
				foreach (BundleMetainfo bundle in bundleRepo.GetAll()) {
					try {
						IEnumerable<JiraIssueModel> jiraIssues = await jiraIssueStorage.Read(bundle.BundleId);
						if (jiraIssues != null) {
							bundleIssues[bundle.BundleId] = jiraIssues.ToList();
						}
					} catch (Exception e) {
						logger.LogError("error reading jira-issue file: " + e.ToString());
						jiraIssueStorage.Wipe(bundle.BundleId);
					}
				}
			} finally {
				IsPopulated = true;
				semaphoreSlim.Release();
			}
		}

		public IEnumerable<JiraIssueModel> GetIssues(string bundleId) {
			return bundleIssues.GetValueOrDefault(bundleId, Enumerable.Empty<JiraIssueModel>().ToList())
				.ToList();
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

		public bool HasBundleOpenIssues(string bundleId) {
			return GetIssues(bundleId).Any(issue => !issue.IsResolved());
		}

		public async Task WipeJiraIssueCache() {
			await semaphoreSlim.WaitAsync().ConfigureAwait(false);
			try {
				foreach (KeyValuePair<string, IList<JiraIssueModel>> item in bundleIssues) {
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
			await BlockIfBundleRepoNotReady("JiraIssueRepository.RefreshAllIssuesAsync");

			await semaphoreSlim.WaitAsync().ConfigureAwait(false);
			try {
				//Only update bundles with unresolved issues
				IEnumerable<KeyValuePair<string, IList<JiraIssueModel>>> bundlesToRefresh =
					bundleIssues.Where(bundle => bundle.Value.Any(issue => issue.GetStatusName() != "Resolved"));

				if (!bundlesToRefresh.Any()) {
					return;
				}

				//Split issues into one group with all resolved issues and one with all others
				ILookup<bool, JiraIssueModel> issuesToRefresh = bundlesToRefresh.SelectMany(issue => issue.Value).
					ToLookup(issue => issue.GetStatusName() != "Resolved");

				//Get the current status of not resolved issues from the jira api and combine them with the resolved issues
				IEnumerable<JiraIssueModel> refreshedIssues = (await apiService.GetBulkIssues(issuesToRefresh[true].Select(i => i.Key)))
						.Union(issuesToRefresh[false], new JiraIssueModel.KeyEqualityComparer());

				await SetBundleIssues(bundlesToRefresh, refreshedIssues);
			} finally {
				semaphoreSlim.Release();
			}
		}

		[Queue("jirastatus")]
		public async Task ForceRefreshAllIssuesAsync() {
			await BlockIfBundleRepoNotReady("JiraIssueRepository.ForceRefreshAllIssuesAsync");

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
		public async Task SearchAllBundleIssues(TimeSpan searchTimeSpan, bool force = false) {
			await BlockIfBundleRepoNotReady("JiraIssueRepository.SearchAllBundleIssues");

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
			await BlockIfBundleRepoNotReady("JiraIssueRepository.SearchBundleIssuesAsync");

			await semaphoreSlim.WaitAsync().ConfigureAwait(false);
			try {
				IEnumerable<BundleMetainfo> bundlesToSearch = force ? bundles : 
					bundles.Where(bundle => !bundleIssues.TryGetValue(bundle.BundleId, out IList<JiraIssueModel> issues) || !issues.Any()); //All bundles without issues

				await Task.WhenAll(bundlesToSearch.Select(bundle => SearchBundleAsync(bundle, force)));
			} finally {
				semaphoreSlim.Release();
			}
		}

		private async Task SearchBundleAsync(BundleMetainfo bundle, bool force) {
			IList<JiraIssueModel> jiraIssues;
			if (!force && bundle.CustomProperties.TryGetValue(settings.CustomPropertyJiraIssueKey, out string jiraIssue)) {
				jiraIssues = new List<JiraIssueModel>() { new JiraIssueModel { Key = jiraIssue } };
			} else {
				jiraIssues = (await apiService.GetJiraIssues(bundle.BundleId)).ToList();
			}
			if (jiraIssues.Any()) {
				bundleIssues[bundle.BundleId] = jiraIssues;
				await jiraIssueStorage.Store(bundle.BundleId, jiraIssues);
			} else {
				bundleIssues.Remove(bundle.BundleId, out var val);
				await jiraIssueStorage.Store(bundle.BundleId, Enumerable.Empty<JiraIssueModel>());
			}
		}

		private async Task SetBundleIssues(IEnumerable<KeyValuePair<string, IList<JiraIssueModel>>> bundlesToUpdate, IEnumerable<JiraIssueModel> refreshedIssues) {
			var issueDictionary = refreshedIssues.ToDictionary(issue => issue.Key, issue => issue);

			//Select the issues for each bundle and store them in the bundleIssues Dictionary
			//I am not sure if this is the best way to do this
			var fileStorageTasks = new List<Task>();
			foreach (KeyValuePair<string, IList<JiraIssueModel>> bundle in bundlesToUpdate) {
				IList<JiraIssueModel> issues = bundle.Value.Select(issue => issueDictionary[issue.Key]).ToList();
				fileStorageTasks.Add(jiraIssueStorage.Store(bundle.Key, bundleIssues[bundle.Key] = issues)); //update the issue file for the bundle
			}

			await Task.WhenAll(fileStorageTasks);
		}

		/// <summary>
		/// Blocks until bundleRepo is fully populated.
		/// </summary>
		private async Task BlockIfBundleRepoNotReady(string sourcemethod) {
			if (!bundleRepo.IsPopulated) {
				Console.WriteLine($"{sourcemethod} is blocked because bundleRepo is not yet fully populated...");
				await Utility.BlockUntil(() => bundleRepo.IsPopulated);
				Console.WriteLine($"...continuing {sourcemethod}.");
			}
		}
	}
}
