using System.Collections.Generic;

namespace SuperDumpService.Models {
	public class JiraSearchResultModel {
		public IEnumerable<JiraIssueModel> Issues { get; set; }
	}
}
