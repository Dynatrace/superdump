using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SuperDumpService.Helpers;
using SuperDumpService.Models;
using SuperDumpService.Services;
using SuperDumpService.Test.Fakes;

namespace SuperDumpService.Benchmark.Benchmarks {
	[ShortRunJob]
	public class SearchServiceBenchmarks {
		private readonly int N;
		private readonly PathHelper pathHelper;
		private readonly FakeDumpStorage dumpStorage;
		private readonly DumpRepository dumpRepo;
		private readonly BundleRepository bundleRepo;
		private readonly FakeIdenticalDumpStorage identicalDumpStorage;
		private readonly IdenticalDumpRepository identicalDumpRepository;
		private readonly LoggerFactory loggerFactory;
		private readonly FakeJiraIssueStorage jiraIssueStorage;
		private readonly FakeJiraApiService jiraApiService;
		private readonly JiraIssueRepository jiraIssueRepository;
		private readonly SearchService searchService;
		private readonly SearchService searchServiceWithJira;

		public SearchServiceBenchmarks() {
			/// fake a repository of N very similar dumps. Then let similarity calculation run
			/// simulate filesystem access with Thread.Sleep in FakeDumpStorage
			N = 100000;

			var settings = Options.Create(new SuperDumpSettings {
				WarnBeforeDeletionInDays = 2,
				UseJiraIntegration = false,
				JiraIntegrationSettings = new JiraIntegrationSettings()
			});

			var settingsWithJira = Options.Create(new SuperDumpSettings {
				WarnBeforeDeletionInDays = 2,
				UseJiraIntegration = true,
				JiraIntegrationSettings = new JiraIntegrationSettings()
			});

			this.pathHelper = new PathHelper("", "", "");
			this.loggerFactory = new LoggerFactory();

			this.dumpStorage = new FakeDumpStorage(CreateFakeDumps(N));
			this.dumpRepo = new DumpRepository(dumpStorage, pathHelper, settings);

			this.bundleRepo = new BundleRepository(dumpStorage, dumpRepo);

			this.identicalDumpStorage = new FakeIdenticalDumpStorage();
			this.identicalDumpRepository = new IdenticalDumpRepository(identicalDumpStorage, bundleRepo);

			this.jiraIssueStorage = new FakeJiraIssueStorage();
			this.jiraApiService = new FakeJiraApiService();
			this.jiraIssueRepository = new JiraIssueRepository(settings, jiraApiService, bundleRepo, jiraIssueStorage, identicalDumpRepository, loggerFactory);

			// The Similarity Service and Elastic Search Service is not initialized since it is not required for testing the SimpleSearch
			this.searchService = new SearchService(bundleRepo, dumpRepo, null, null, settings, jiraIssueRepository);
			this.searchServiceWithJira = new SearchService(bundleRepo, dumpRepo, null, null, settingsWithJira, jiraIssueRepository);
		}

		[GlobalSetup]
		public async Task GlobalSetup() {
			// populate in-memory repository from fake storage
			for (int i = 0; i < N; i++) {
				await this.dumpRepo.PopulateForBundle($"bundle{i}");
			}
			this.dumpRepo.SetIsPopulated();

			await bundleRepo.Populate();

			// create fake jira issues and populate the repository
			CreateFakeJiraIssues();
			await jiraIssueRepository.Populate();
			await jiraIssueRepository.SearchBundleIssuesAsync(bundleRepo.GetAll(), true);

			// enable storage delay simulation
			this.dumpStorage.DelaysEnabled = true;
		}

		private IEnumerable<FakeDump> CreateFakeDumps(int n) {
			var fakeDumps = new List<FakeDump>();
			for (int i = 0; i < n; i++) {
				fakeDumps.Add(new FakeDump {
					MetaInfo = new DumpMetainfo { BundleId = $"bundle{i}", DumpId = $"dump{i}", Status = DumpStatus.Finished }
				});
			}
			return fakeDumps;
		}

		private void CreateFakeJiraIssues() {
			// initialize the Random with fixed seed for reproducability
			var random = new Random(0);
			int issueId = 0;
			foreach (BundleMetainfo bundle in bundleRepo.GetAll()) {
				int n = random.Next(4);
				var jiraIssues = new JiraIssueModel[n];
				for (int i = 0; i < n; i++) {
					string resolution = random.Next(2) == 0 ? "Open" : "Resolved";
					jiraIssues[i] = new JiraIssueModel {
						Key = $"JRA-{issueId++}",
						Fields = new JiraIssueModel.JiraIssueFieldModel {
							Resolution = new JiraIssueModel.JiraIssueStatusModel {
								Id = resolution, Name = resolution
							}
						}
					};
				}
				jiraApiService.SetFakeJiraIssues(bundle.BundleId, jiraIssues);
			}
		}

		/// <summary>
		/// Measures the performance of the SearchService without enabled JiraIntegration
		/// </summary>
		/// <returns></returns>
		[Benchmark]
		public async Task SearchServiceAsync() {
			await searchService.SearchBySimpleFilter("", false);
		}

		/// <summary>
		/// Measures the performance of the SearchService with enabled Jira Integration to test the performance impact of the HasBundleOpenIssues() method
		/// </summary>
		/// <returns></returns>
		[Benchmark]
		public async Task SearchServiceWithJiraAsync() {
			await searchServiceWithJira.SearchBySimpleFilter("", false);
		}
	}
}
