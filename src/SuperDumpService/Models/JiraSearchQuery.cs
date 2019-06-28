namespace SuperDumpService.Models {
	public class JiraSearchQuery {
		public string Jql { get; set; }
		public string[] Fields { get; set; }
		public int StartAt { get; set; }
	}
}
