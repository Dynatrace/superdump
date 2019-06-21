using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SuperDump.Models;
using SuperDumpService.Test.Fakes;
using SuperDumpService.Helpers;
using SuperDumpService.Models;
using SuperDumpService.Services;
using Xunit;
using Microsoft.Extensions.Logging;
using Xunit.Sdk;

namespace SuperDumpService.Test {

	public class JiraIssueRepositoryTest {

		[Fact]
		public async Task TestFakeJiraIssueApiService() {
			var jiraApiService = new FakeJiraApiService();
			jiraApiService.SetFakeJiraIssues("bundle1", new JiraIssueModel[] { new JiraIssueModel("JRA-1111") });
			jiraApiService.SetFakeJiraIssues("bundle2", new JiraIssueModel[] { new JiraIssueModel("JRA-2222"), new JiraIssueModel("JRA-22221") });
			jiraApiService.SetFakeJiraIssues("bundle3", new JiraIssueModel[] { new JiraIssueModel("JRA-1111"), new JiraIssueModel("JRA-3333") });

			Assert.Collection((await jiraApiService.GetJiraIssues("bundle1")).OrderBy(x => x.Key),
				item => Assert.Equal("JRA-1111", item.Key));
			Assert.Collection((await jiraApiService.GetJiraIssues("bundle2")).OrderBy(x => x.Key),
				item => Assert.Equal("JRA-2222", item.Key),
				item => Assert.Equal("JRA-22221", item.Key));
			Assert.Collection((await jiraApiService.GetJiraIssues("bundle3")).OrderBy(x => x.Key),
				item => Assert.Equal("JRA-1111", item.Key),
				item => Assert.Equal("JRA-3333", item.Key));

			
			Assert.Collection((await jiraApiService.GetBulkIssues(new string[] { "JRA-1111" })).OrderBy(x => x.Key),
				item => Assert.Equal("JRA-1111", item.Key));

			Assert.Collection((await jiraApiService.GetBulkIssues(new string[] { "JRA-1111", "JRA-2222", "JRA-3333" })).OrderBy(x => x.Key),
				item => Assert.Equal("JRA-1111", item.Key),
				item => Assert.Equal("JRA-2222", item.Key),
				item => Assert.Equal("JRA-3333", item.Key));
		}

		[Fact]
		public async Task TestJiraIssueRepository() {
			// setup dependencies
			InitFakeJiraRepository(out FakeJiraApiService jiraApiService,
				out FakeJiraIssueStorage jiraIssueStorage,
				out JiraIssueRepository jiraIssueRepository,
				out BundleRepository bundleRepository);

			// population
			await jiraIssueStorage.Store("bundle1", new List<JiraIssueModel> { new JiraIssueModel("JRA-1111") });
			await jiraIssueStorage.Store("bundle2", new List<JiraIssueModel> { new JiraIssueModel("JRA-2222"), new JiraIssueModel("JRA-3333") });
			await jiraIssueStorage.Store("bundle9", new List<JiraIssueModel> { new JiraIssueModel("JRA-9999") });

			await bundleRepository.Populate();
			await jiraIssueRepository.Populate();

			// actual test

			Assert.Collection(jiraIssueRepository.GetIssues("bundle1"),
				item => Assert.Equal("JRA-1111", item.Key));

			Assert.Collection(jiraIssueRepository.GetIssues("bundle2"),
				item => Assert.Equal("JRA-2222", item.Key),
				item => Assert.Equal("JRA-3333", item.Key));

			Assert.Collection(jiraIssueRepository.GetIssues("bundle9"),
				item => Assert.Equal("JRA-9999", item.Key));

			Assert.Empty(jiraIssueRepository.GetIssues("bundle3"));

			// fake, that in jira some bundles have been referenced in new issues
			jiraApiService.SetFakeJiraIssues("bundle1", new JiraIssueModel[] { new JiraIssueModel("JRA-1111") }); // same
			jiraApiService.SetFakeJiraIssues("bundle2", new JiraIssueModel[] { new JiraIssueModel("JRA-2222"), new JiraIssueModel("JRA-4444") }); // one added, one removed
			jiraApiService.SetFakeJiraIssues("bundle3", new JiraIssueModel[] { new JiraIssueModel("JRA-1111"), new JiraIssueModel("JRA-5555") }); // new
			jiraApiService.SetFakeJiraIssues("bundle9", null ); // removed jira issues

			await jiraIssueRepository.SearchBundleIssuesAsync(bundleRepository.GetAll(), true);

			Assert.Collection((jiraIssueRepository.GetIssues("bundle1")).OrderBy(x => x.Key),
				item => Assert.Equal("JRA-1111", item.Key));

			Assert.Collection((jiraIssueRepository.GetIssues("bundle2")).OrderBy(x => x.Key),
				item => Assert.Equal("JRA-2222", item.Key),
				item => Assert.Equal("JRA-4444", item.Key));

			Assert.Collection((jiraIssueRepository.GetIssues("bundle3")).OrderBy(x => x.Key),
				item => Assert.Equal("JRA-1111", item.Key),
				item => Assert.Equal("JRA-5555", item.Key));

			Assert.Empty(jiraIssueRepository.GetIssues("bundle9"));

			var res = await jiraIssueRepository.GetAllIssuesByBundleIdsWithoutWait(new string[] { "bundle1", "bundle2", "bundle7", "bundle666", "bundle9" });
			Assert.Equal(2, res.Count());

			Assert.Collection(res["bundle1"].OrderBy(x => x.Key),
				item => Assert.Equal("JRA-1111", item.Key));

			Assert.Collection(res["bundle2"].OrderBy(x => x.Key),
				item => Assert.Equal("JRA-2222", item.Key),
				item => Assert.Equal("JRA-4444", item.Key));


			Assert.Empty(jiraIssueRepository.GetIssues("bundle7"));
			Assert.Empty(jiraIssueRepository.GetIssues("bundle666"));
			Assert.Empty(jiraIssueRepository.GetIssues("bundle9"));
		}

		[Fact]
		public async Task TestRefreshJiraIssues() {
			InitFakeJiraRepository(out FakeJiraApiService jiraApiService,
				out FakeJiraIssueStorage jiraIssueStorage,
				out JiraIssueRepository jiraIssueRepository,
				out BundleRepository bundleRepository);

			// population
			await jiraIssueStorage.Store("bundle1", new List<JiraIssueModel> { new JiraIssueModel("JRA-1111", "Open") });
			await jiraIssueStorage.Store("bundle2", new List<JiraIssueModel> { new JiraIssueModel("JRA-2222"), new JiraIssueModel("JRA-3333") });
			await jiraIssueStorage.Store("bundle9", new List<JiraIssueModel> { new JiraIssueModel("JRA-1111", "Resolved"), new JiraIssueModel("JRA-9999") });

			await bundleRepository.Populate();
			await jiraIssueRepository.Populate();

			jiraApiService.SetFakeJiraIssues("bundle1", new JiraIssueModel[] { new JiraIssueModel("JRA-1111", "Resolved")});
			jiraApiService.SetFakeJiraIssues("bundle2", new JiraIssueModel[] { new JiraIssueModel("JRA-2222"), new JiraIssueModel("JRA-3333") });
			jiraApiService.SetFakeJiraIssues("bundle3", new JiraIssueModel[] { new JiraIssueModel("JRA-1111", "Resolved"), new JiraIssueModel("JRA-9999") });

			await jiraIssueRepository.RefreshAllIssuesAsync();

			Assert.Collection(await jiraIssueRepository.GetAllIssuesByBundleIdsWithoutWait(new string[] { "bundle1" }),
				item => {
					Assert.Equal("JRA-1111", item.Value.Single().Key);
					Assert.True(item.Value.Single().IsResolved());
				});
		}

		private void InitFakeJiraRepository(out FakeJiraApiService fakeJiraApiService,
				out FakeJiraIssueStorage fakeJiraIssueStorage,
				out JiraIssueRepository jiraIssueRepository,
				out BundleRepository bundleRepository) {

			var settings = Options.Create(new SuperDumpSettings {
				UseJiraIntegration = true,
				JiraIntegrationSettings = new JiraIntegrationSettings { }
			});
			var pathHelper = new PathHelper("", "", "");
			var dumpStorage = new FakeDumpStorage(CreateFakeDumps());
			var bundleStorage = dumpStorage;
			var dumpRepo = new DumpRepository(dumpStorage, pathHelper, settings);
			bundleRepository = new BundleRepository(bundleStorage, dumpRepo);

			fakeJiraApiService = new FakeJiraApiService();
			fakeJiraIssueStorage = new FakeJiraIssueStorage();

			var identicalDumpStorage = new FakeIdenticalDumpStorage();
			var identicalDumpRepository = new IdenticalDumpRepository(identicalDumpStorage, bundleRepository);

			var loggerFactory = new LoggerFactory();

			jiraIssueRepository = new JiraIssueRepository(settings, fakeJiraApiService, bundleRepository, fakeJiraIssueStorage, identicalDumpRepository, loggerFactory);
		}

		private IEnumerable<FakeDump> CreateFakeDumps() {
			int n = 10;
			for (int i = 0; i < n; i++) {
				var res = new SDResult { ThreadInformation = new Dictionary<uint, SDThread>() };
				yield return new FakeDump {
					MetaInfo = new DumpMetainfo { BundleId = $"bundle{i}", DumpId = $"dump{i}", Status = DumpStatus.Finished },
					FileInfo = null,
					Result = res,
					MiniInfo = CrashSimilarity.SDResultToMiniInfo(res)
				};
			}
		}

	}
}
