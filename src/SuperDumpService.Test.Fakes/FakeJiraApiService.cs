using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SuperDumpService.Models;
using SuperDumpService.Services;

namespace SuperDumpService.Test.Fakes {
	public class FakeJiraApiService : IJiraApiService {
		private readonly ConcurrentDictionary<string, IEnumerable<JiraIssueModel>> jiraIssueStore = new ConcurrentDictionary<string, IEnumerable<JiraIssueModel>>();
		private readonly object sync = new object();

		public void SetFakeJiraIssues(string bundleId, IEnumerable<JiraIssueModel> jiraIssueModels) {
			jiraIssueStore[bundleId] = jiraIssueModels;
		}

		public Task<IEnumerable<JiraIssueModel>> GetBulkIssues(IEnumerable<string> issueKeys) {
			return Task.FromResult<IEnumerable<JiraIssueModel>>(
				jiraIssueStore.SelectMany(x => x.Value)
				.Where(x => issueKeys.Contains(x.Key))
				.GroupBy(x => x.Key)
				.Select(group => group.First()));
		}

		public Task<IEnumerable<JiraIssueModel>> GetJiraIssues(string bundleId) {
			return Task.FromResult<IEnumerable<JiraIssueModel>>(jiraIssueStore[bundleId]);
		}
	}
}
