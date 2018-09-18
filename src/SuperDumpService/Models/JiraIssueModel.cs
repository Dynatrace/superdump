using System;
using System.Collections.Generic;

namespace SuperDumpService.Models {
	public class JiraIssueModel {
		public string Id { get; set; }
		public string Key { get; set; }
		public string Url { get; set; }
		public JiraIssueFieldModel Fields { get; set; }

		public string GetStatusName() {
			return Fields != null && Fields.Status != null ? Fields.Status.Name : null;
		}

		public string GetResolutionName() {
			return Fields != null && Fields.Resolution != null ? Fields.Resolution.Name : null;
		}

		public class JiraIssueFieldModel {
			public JiraIssueStatusModel Status { get; set; }

			public JiraIssueStatusModel Resolution { get; set; }
		}

		public class JiraIssueStatusModel {
			public string Name { get; set; }
			public string Id { get; set; }
		}
	}
}
