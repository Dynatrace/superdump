using System;
using System.Collections.Generic;

namespace SuperDumpService.Models {
	public class JiraIssueModel {
		private const string JiraIssueStatusResolved = "Resolved";

		public string Id { get; set; }
		public string Key { get; set; }
		public string Url { get; set; }
		public JiraIssueFieldModel Fields { get; set; }

		public JiraIssueModel() { }
		public JiraIssueModel(string key) {
			this.Key = key;
		}

		public string GetStatusName() {
			return Fields != null && Fields.Status != null ? Fields.Status.Name : null;
		}

		public string GetResolutionName() {
			return Fields != null && Fields.Resolution != null ? Fields.Resolution.Name : null;
		}

		public bool IsResolved() {
			return GetStatusName() == JiraIssueStatusResolved;
		}

		public class JiraIssueFieldModel {
			public JiraIssueStatusModel Status { get; set; }

			public JiraIssueStatusModel Resolution { get; set; }
		}

		public class JiraIssueStatusModel {
			public string Name { get; set; }
			public string Id { get; set; }
		}

		public class KeyEqualityComparer : IEqualityComparer<JiraIssueModel> {
			public bool Equals(JiraIssueModel x, JiraIssueModel y) {
				return x.Key.Equals(y.Key);
			}

			public int GetHashCode(JiraIssueModel obj) {
				return obj.Key.GetHashCode();
			}
		}

		public override string ToString() {
			return Key;
		}
	}
}
