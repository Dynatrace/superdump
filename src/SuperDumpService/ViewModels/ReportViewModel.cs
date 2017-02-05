using SuperDump.Models;
using SuperDumpService.Models;
using System;
using System.Collections.Generic;

namespace SuperDumpService.ViewModels {
	public class ReportViewModel {
		public string BundleId { get; set; }
		public string Id { get; set; }
		public string JiraIssue { get; set; }
		public string FriendlyName { get; set; }
		public string Url { get; set; }
		public DateTime TimeStamp { get; set; }
		public SDResult Result { get; set; }
		public bool HasAnalysisFailed { get; set; }
		public string AnalysisError { get; set; }
		public IEnumerable<string> Files { get; set; }
		public ISet<SDTag> ThreadTags { get; set; }
		public int PointerSize { get; set; }

		public ReportViewModel(string bundleId, string id) {
			this.BundleId = bundleId;
			this.Id = id;
		}

		public ReportViewModel(string bundleId,
								string id,
								string jira,
								string friendlyName,
								string url,
								DateTime timeStamp,
								SDResult res,
								bool failed,
								string analysisError,
								IEnumerable<string> files) {
			this.BundleId = bundleId;
			this.Id = id;
			this.JiraIssue = jira;
			this.FriendlyName = friendlyName;
			this.Url = url;
			this.TimeStamp = timeStamp;
			this.HasAnalysisFailed = failed;
			this.AnalysisError = analysisError;
			this.Result = res;
			this.Files = files;
			this.ThreadTags = GetThreadTags(res);
			this.PointerSize = res == null ? 8 : (res.SystemContext.ProcessArchitecture == "X86" ? 8 : 12);
		}

		private ISet<SDTag> GetThreadTags(SDResult res) {
			var tags = new HashSet<SDTag>();
			if (res == null || res.ThreadInformation == null) return tags;
			foreach (var thread in res.ThreadInformation) {
				if (thread.Value.Tags == null) continue;
				foreach (var tag in thread.Value.Tags) {
					tags.Add(tag);
				}
			}
			return tags;
		}

		private static readonly string[] sizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
		public string SizeSuffix(ulong value) {
			if (value == 0) { return "0 bytes"; }

			int mag = (int)Math.Log(value, 1024);
			decimal adjustedSize = (decimal)value / (1L << (mag * 10));

			return string.Format("{0:n1}{1}", adjustedSize, sizeSuffixes[mag]).Replace(",", ".");
		}
	}
}
