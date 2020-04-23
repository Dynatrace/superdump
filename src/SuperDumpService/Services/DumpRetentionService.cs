using Hangfire;
using Microsoft.Extensions.Options;
using System;
using SuperDumpService.Models;
using SuperDumpService.Helpers;
using System.IO;

namespace SuperDumpService.Services {
	public class DumpRetentionService {
		private const string RETENTION_JOB_ID = "dump-retention";
		private const string JiraRetentionExtensionReason = "The retention time was extended due to an open jira issue.";

		private readonly DumpRepository dumpRepo;
		private readonly BundleRepository bundleRepo;
		private readonly PathHelper pathHelper;
		private readonly JiraIssueRepository jiraIssueRepository;
		private readonly SuperDumpSettings settings;

		public DumpRetentionService(DumpRepository dumpRepo, BundleRepository bundleRepo, PathHelper pathHelper, IOptions<SuperDumpSettings> settings, JiraIssueRepository jiraIssueRepository) {
			this.dumpRepo = dumpRepo ?? throw new ArgumentNullException("Dump Repository must not be null!");
			this.bundleRepo = bundleRepo ?? throw new ArgumentNullException("Bundle Repository must not be null!");
			this.pathHelper = pathHelper ?? throw new ArgumentException("PathHelper must not be null!");
			this.jiraIssueRepository = jiraIssueRepository;
			this.settings = settings?.Value ?? throw new ArgumentException("Settings must not be null!");
		}

		public void StartService() {
			if (!settings.IsDumpRetentionEnabled()) {
				return;
			}
			RecurringJob.AddOrUpdate(() => RemoveOldDumps(), settings.DumpRetentionCron, null, "retention");
		}

		[Hangfire.Queue("retention", Order = 2)]
		public void RemoveOldDumps() {
			// Check if the jiraIssueRepository is populated to avoid deleting dumps with open issues
			if (settings.UseJiraIntegration && !jiraIssueRepository.IsPopulated) {
				return;
			}
			var jiraExtensionTime = settings.UseJiraIntegration ? TimeSpan.FromDays(settings.JiraIntegrationSettings.JiraDumpRetentionTimeExtensionDays) : TimeSpan.Zero;

			foreach (var bundle in bundleRepo.GetAll()) {
				if (bundle == null) continue;
				foreach (var dump in dumpRepo.Get(bundle.BundleId)) {
					if (dump == null) continue;
					if (settings.UseJiraIntegration && jiraIssueRepository.HasBundleOpenIssues(bundle.BundleId)) {
						// do not set the dump deletion date if it would shorten the current retention time
						if (jiraExtensionTime > dump.PlannedDeletionDate - DateTime.Now) {
							dumpRepo.SetPlannedDeletionDate(dump.Id, DateTime.Now + jiraExtensionTime, JiraRetentionExtensionReason);
						}
					} else if (dump.PlannedDeletionDate < DateTime.Now) {
						RemoveOldDumps(dump);
					}
				}
			}
		}

		/// <summary>
		/// Deletes dump files from dumps that are older than the configured retention time.
		/// There is no exception handling because exceptions are visible in Hangfire as opposed to log messages.
		/// </summary>
		private void RemoveOldDumps(DumpMetainfo dump) {
			string dumpDirectory = pathHelper.GetDumpDirectory(dump.Id);
			if (!Directory.Exists(dumpDirectory)) {
				return;
			}
			Console.WriteLine($"[DumpRetention] Deleting dump {dump.BundleId}/{dump.DumpId}");
			// Delete all directories in the dump directory
			foreach (var subdir in Directory.EnumerateDirectories(dumpDirectory)) {
				Directory.Delete(subdir, true);
			}
			// Delete all dump files in the dump directory
			foreach (var file in Directory.EnumerateFiles(dumpDirectory)) {
				if (file.EndsWith(".core.gz", StringComparison.OrdinalIgnoreCase) 
					|| file.EndsWith("libs.tar.gz", StringComparison.OrdinalIgnoreCase) 
					|| file.EndsWith(".dmp", StringComparison.OrdinalIgnoreCase)) {

					File.Delete(file);
				}
			}
			dumpRepo.UpdateIsDumpAvailable(dump.Id);
		}
	}
}
