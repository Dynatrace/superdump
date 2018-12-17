using System.Collections.Generic;
using System.Threading.Tasks;
using SuperDumpService.Models;

namespace SuperDumpService.Services {
	public interface IJiraIssueStorage {
		Task<IEnumerable<JiraIssueModel>> Read(string bundleId);
		Task Store(string bundleId, IEnumerable<JiraIssueModel> jiraIssues);
		void Wipe(string bundleId);
	}
}