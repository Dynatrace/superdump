using System.Collections.Generic;
using System.Threading.Tasks;
using SuperDumpService.Models;

namespace SuperDumpService.Services {
	public interface IJiraApiService {
		Task<IEnumerable<JiraIssueModel>> GetBulkIssues(IEnumerable<string> issueKeys);
		Task<IEnumerable<JiraIssueModel>> GetJiraIssues(string bundleId);
	}
}