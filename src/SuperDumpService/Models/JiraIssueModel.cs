using System;

namespace SuperDumpService.Models {
	public class JiraIssueModel {
		public string Id { get; set; }
		public string Key { get; set; }
		public JiraIssueFieldModel Fields { get; set; }

		public string StatusName {
			get {
				return Fields != null && Fields.Status != null ? Fields.Status.Name : null;
			}
		}

		public class JiraIssueFieldModel {
			public JiraIssueStatusModel Status { get; set; }
		}

		public class JiraIssueStatusModel {
			public string Name { get; set; }
			public string Id { get; set; }
		}
	}
}
