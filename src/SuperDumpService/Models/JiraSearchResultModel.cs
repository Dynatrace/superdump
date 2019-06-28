using System.Collections.Generic;

namespace SuperDumpService.Models {
	public class JiraSearchResultModel {
		public int StartAt { get; set; }
		public int MaxResults { get; set; }
		public int Total { get; set; }
		public IEnumerable<JiraIssueModel> Issues { get; set; }
	}
}
