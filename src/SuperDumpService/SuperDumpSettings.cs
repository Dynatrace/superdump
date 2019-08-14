using System;
using System.Collections.Generic;

namespace SuperDumpService {
	public class SuperDumpSettings {
		public string LocalSymbolCache { get; set; }
		public string SymStoreExex86 { get; set; }
		public string SymStoreExex64 { get; set; }
		public int MaxConcurrentAnalysis { get; set; }
		public int MaxConcurrentBundleExtraction { get; set; }
		public string DumpsDir { get; set; }
		public string UploadDir { get; set; }
		public string HangfireLocalDbDir { get; set; }
		public string SuperDumpSelectorExePath { get; set; }
		public bool DeleteDumpAfterAnalysis { get; set; }
		public bool DumpDownloadable { get; set; } = true;
		public int MaxUploadSizeMB { get; set; } = 16000;
		public int DumpRetentionDays { get; set; } = 0;
		public int WarnBeforeDeletionInDays { get; set; } = 0;
		public int DumpRetentionExtensionDays { get; set; } = 30;
		public string DumpRetentionCron { get; set; } = "";
		public bool IncludeOtherFilesInReport { get; set; }
		public IEnumerable<string> BinPath { get; set; }
		public string WindowsInteractiveCommandx86 { get; set; }
		public string WindowsInteractiveCommandx64 { get; set; }
		public string LinuxAnalysisCommand { get; set; }
		public string LinuxInteractiveCommand { get; set; }
		public string[] SlackNotificationUrls { get; set; }
		public string SuperDumpUrl { get; set; }
		public string RepositoryUrl { get; set; }
		public string InteractiveGdbHost { get; set; }
		public string ElasticSearchHost { get; set; }
		public string NodeJsPath { get; set; }
		public bool SimilarityDetectionEnabled { get; set; }
		public int SimilarityDetectionMaxDays { get; set; } = 90;
		public bool UseLdapAuthentication { get; set; }
		public bool UseHttpsRedirection { get; set; }
		public LdapAuthenticationSettings LdapAuthenticationSettings { get; set; }
		public bool UseJiraIntegration { get; set; }
		public JiraIntegrationSettings JiraIntegrationSettings { get; set; }
		public bool UseAllRequestLogging { get; set; }
		public bool DuplicationDetectionEnabled { get; set; }
		public int DownloadServiceRetry { get; set; } = 3;
		public int DownloadServiceRetryTimeout { get; set; } = 500;

        public bool IsDumpRetentionEnabled () {
			return !string.IsNullOrEmpty(DumpRetentionCron) && DumpRetentionDays > 0;
		}
	}
}
