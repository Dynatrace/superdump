using System;

namespace SuperDump.Models {
	public class DumpInfo {
		public string FileName { get; set; }
		public string Path { get; set; }
		public string JiraIssue { get; set; }
		public string FriendlyName { get; set; }
		public DateTime ServerTimeStamp { get; set; }
	}
}
