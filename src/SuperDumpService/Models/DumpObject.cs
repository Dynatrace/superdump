using System;

namespace SuperDumpService.Models {
	public class DumpObject {
		public string Path { get; set; }
		public string Report { get; set; }
		public string JiraIssue { get; set; }
		public string FriendlyName { get; set; }
		public string Id { get; set; }
		public DateTime TimeStamp { get; set; }
		public bool IsAnalysisComplete { get; set; }
		public bool HasAnalysisFailed { get; set; }
		public string AnalysisError { get; set; }
	}
}
