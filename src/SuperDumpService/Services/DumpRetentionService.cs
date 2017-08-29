using Hangfire;
using Microsoft.Extensions.Options;
using System;
using SuperDumpService.Models;
using SuperDumpService.Helpers;
using System.IO;

namespace SuperDumpService.Services {
	public class DumpRetentionService {
		private const string RETENTION_JOB_ID = "dump-retention";

		private readonly DumpRepository dumpRepo;
		private readonly BundleRepository bundleRepo;
		private readonly PathHelper pathHelper;
		private readonly SuperDumpSettings settings;

		public DumpRetentionService(DumpRepository dumpRepo, BundleRepository bundleRepo, PathHelper pathHelper, IOptions<SuperDumpSettings> settings) {
			this.dumpRepo = dumpRepo ?? throw new ArgumentNullException("Dump Repository must not be null!");
			this.bundleRepo = bundleRepo ?? throw new ArgumentNullException("Bundle Repository must not be null!");
			this.pathHelper = pathHelper ?? throw new ArgumentException("PathHelper must not be null!");
			this.settings = settings?.Value ?? throw new ArgumentException("Settings must not be null!");
		}

		public void StartService() {
			if(string.IsNullOrEmpty(settings.DumpRetentionCron) || settings.DumpRetentionDays == 0) {
				return;
			}
			RecurringJob.AddOrUpdate(() => RemoveOldDumps(), settings.DumpRetentionCron, null, "retention");
		}

		[Hangfire.Queue("retention", Order = 2)]
		public void RemoveOldDumps() {
			foreach(var bundle in bundleRepo.GetAll()) {
				foreach(var dump in dumpRepo.Get(bundle.BundleId)) {
					if(dump.Created < DateTime.Now.Subtract(TimeSpan.FromDays(settings.DumpRetentionDays))) {
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
			string dumpDirectory = pathHelper.GetDumpDirectory(dump.BundleId, dump.DumpId);
			if(!Directory.Exists(dumpDirectory)) {
				return;
			}
			Console.WriteLine($"[DumpRetention] Deleting dump {dump.BundleId}/{dump.DumpId}");
			// Delete all directories in the dump directory
			foreach(var subdir in Directory.EnumerateDirectories(dumpDirectory)) {
				Directory.Delete(subdir, true);
			}
			// Delete all dump files in the dump directory
			foreach(var file in Directory.EnumerateFiles(dumpDirectory)) {
				if(file.EndsWith(".core.gz") || file.EndsWith("libs.tar.gz") || file.EndsWith(".dmp")) {
					File.Delete(file);
				}
			}
		}
	}
}
