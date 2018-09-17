using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService {
	public class JiraIntegrationSettings {
		public string JiraIssueRefreshCron { get; set; }
		public string JiraBundleIssueSearchCron { get; set; }
		public string CustomPropertyJiraIssueKey { get; set; }
		public int JiraBundleSearchLimit { get; set; }
		public double JiraBundleSearchDelay { get; set; }
		public TimeSpan JiraBundleSearchTimeSpan { get; set; }
		public string JiraApiSearchUrl { get; set; }
		public string JiraApiUsername { get; set; }
		public string JiraApiPassword { get; set; }
		public string JiraIssueUrl { get; set; }
	}
}
