using System.Collections.Concurrent;

namespace SuperDumpService.Models {
	public class DumpBundle : DumpObject {
		public string Url { get; set; }
		public string UrlFilename { get; set; } // in case of Web-URL, this must be the filename of the file to be downloaded
		public bool DownloadCompleted { get; set; }
		public ConcurrentDictionary<string, DumpAnalysisItem> DumpItems { get; set; } = new ConcurrentDictionary<string, DumpAnalysisItem>();

		public DumpBundle() : base() { }

		public DumpBundle(string id) {
			this.Id = id;
		}

		public DumpBundle(string jiraIssue, string friendlyName, string url) {
			this.JiraIssue = jiraIssue;
			this.FriendlyName = friendlyName;
			this.Url = url;
		}
	}
}
