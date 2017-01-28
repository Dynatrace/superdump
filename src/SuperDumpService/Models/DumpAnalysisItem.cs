using System;
using System.Collections.Generic;

namespace SuperDumpService.Models {
	public class DumpAnalysisItem : DumpObject {
		public string BundleId { get; set; }
		public string ResultPath { get; set; }
		public bool AnomaliesDetected { get; set; } = false;
		public IEnumerable<string> Files { get; set; } = new List<string>();

		public string FileName {
			get {
				if (!string.IsNullOrEmpty(this.Path)) {
					return System.IO.Path.GetFileName(this.Path);
				} else {
					return string.Empty;
				}
			}
		}

		public DumpAnalysisItem() { }

		public DumpAnalysisItem(string bundleId, string path, string jira) {
			this.BundleId = bundleId;
			this.Path = path;
			this.JiraIssue = jira;
		}

		public DumpAnalysisItem(string path, string resultPath, DateTime time){
			this.Path = path;
			this.ResultPath = resultPath;
			this.TimeStamp = time;
		}

		public DumpAnalysisItem(string id) {
			this.Id = id;
		}

		public DumpAnalysisItem(string bundleId, string id) {
			this.BundleId = bundleId;
			this.Id = id;
		}
	}
}
