using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SuperDumpService.Models;
using SuperDumpService.Services;

namespace SuperDumpService.Test.Fakes {
	public class FakeJiraIssueStorage : IJiraIssueStorage {
		private ConcurrentDictionary<string, IEnumerable<JiraIssueModel>> jiraIssuesStore = new ConcurrentDictionary<string, IEnumerable<JiraIssueModel>>();

		public Task<IEnumerable<JiraIssueModel>> Read(string bundleId) {
			if (!jiraIssuesStore.ContainsKey(bundleId)) return Task.FromResult<IEnumerable<JiraIssueModel>>(null);
			return Task.FromResult<IEnumerable<JiraIssueModel>>(jiraIssuesStore[bundleId]);
		}

		public Task Store(string bundleId, IEnumerable<JiraIssueModel> jiraIssues) {
			jiraIssuesStore[bundleId] = jiraIssues;
			return Task.CompletedTask;
		}

		public void Wipe(string bundleId) {
			jiraIssuesStore.Clear();
		}
	}
}
