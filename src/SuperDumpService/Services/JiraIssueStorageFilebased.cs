using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SuperDumpService.Helpers;
using SuperDumpService.Models;

namespace SuperDumpService.Services {
	public class JiraIssueStorageFilebased {
		private readonly PathHelper pathHelper;

		public JiraIssueStorageFilebased(PathHelper pathHelper) {
			this.pathHelper = pathHelper;
		}

		public async Task Store(string bundleId, IEnumerable<JiraIssueModel> jiraIssues) {
			string path = pathHelper.GetJiraIssuePath(bundleId);

			await File.WriteAllTextAsync(path, JsonConvert.SerializeObject(jiraIssues));
		}

		public async Task<IEnumerable<JiraIssueModel>> Read(string bundleId) {
			string path = pathHelper.GetJiraIssuePath(bundleId);
			if (!File.Exists(path)) {
				return null;
			}
			string text = await File.ReadAllTextAsync(path);
			return JsonConvert.DeserializeObject<IEnumerable<JiraIssueModel>>(text);
		}

		public void Wipe(string bundleId) {
			File.Delete(pathHelper.GetJiraIssuePath(bundleId));
		}
	}
}
