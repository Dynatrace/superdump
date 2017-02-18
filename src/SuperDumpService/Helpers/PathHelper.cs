using Microsoft.Extensions.Options;
using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace SuperDumpService.Helpers {
	public class PathHelper {
		private readonly string workingDir;
		private readonly string uploadsDir;
		private readonly string hangfireDbDir;
		private readonly string superDumpSelectorExePath;
		private static string confDir = Path.Combine(Directory.GetCurrentDirectory(), @"../../conf/");
		private static string confDirFallback = Directory.GetCurrentDirectory();

		public PathHelper(IConfigurationSection configurationSection) {
			// maybe there is a smarter way to convert IConfigurationSection to SuperDumpSettings?
			this.workingDir = configurationSection.GetValue<string>(nameof(SuperDumpSettings.DumpsDir)) ?? Path.Combine(Directory.GetCurrentDirectory(), @"../../data/dumps/");
			this.uploadsDir = configurationSection.GetValue<string>(nameof(SuperDumpSettings.UploadDir)) ?? Path.Combine(Directory.GetCurrentDirectory(), @"../../data/uploads/");
			this.hangfireDbDir = configurationSection.GetValue<string>(nameof(SuperDumpSettings.HangfireLocalDbDir)) ?? Path.Combine(Directory.GetCurrentDirectory(), @"../../data/hangfire/");
			this.superDumpSelectorExePath = configurationSection.GetValue<string>(nameof(SuperDumpSettings.SuperDumpSelectorExePath)) ?? GetDumpSelectorExePathFallback();
		}

		internal static string GetConfDirectory() {
			if (Directory.Exists(confDir)) {
				return confDir;
			} else {
				return confDirFallback;
			}
		}

		internal string GetHangfireDBDir() {
			return hangfireDbDir;
		}

		public string GetUploadsDir() {
			return Path.GetFullPath(uploadsDir);
		}

		public string GetTempDir() {
			return Path.Combine(GetUploadsDir(), RandomIdGenerator.GetRandomId(3, 16));
		}

		public string GetWorkingDir() {
			return Path.GetFullPath(workingDir);
		}

		public string GetDumpDirectory(string bundleId, string dumpId) {
			return Path.Combine(GetBundleDirectory(bundleId), dumpId);
		}

		public string GetBundleDirectory(string bundleId) {
			return Path.Combine(GetWorkingDir(), bundleId);
		}

		public string GetBundleDownloadPath(string fileName) {
			return Path.Combine(GetUploadsDir(), fileName);
		}

		/// <summary>
		/// makes sure all required directories exist
		/// </summary>
		internal void PrepareDirectories() {
			Directory.CreateDirectory(GetWorkingDir());
			Directory.CreateDirectory(GetUploadsDir());
			Directory.CreateDirectory(GetHangfireDBDir());
		}

		public string GetJsonPath(string bundleId, string dumpId) {
			return Path.Combine(GetDumpDirectory(bundleId, dumpId), "superdump-result.json");
		}

		internal string GetJsonPathFallback(string bundleId, string dumpId) {
			return Path.Combine(GetDumpDirectory(bundleId, dumpId), dumpId + ".json");
		}

		public string GetDumpSelectorExePath() {
			return superDumpSelectorExePath;
		}

		private string GetDumpSelectorExePathFallback() {
			string dumpselector = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\SuperDumpSelector\bin\SuperDumpSelector.exe"));
			if (!File.Exists(dumpselector)) {
				// deployment case
				dumpselector = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\SuperDumpSelector\SuperDumpSelector.exe"));
			}
			return dumpselector;
		}

		internal string GetBundleMetadataPath(string bundleId) {
			return Path.Combine(GetBundleDirectory(bundleId), "bundleinfo.json");
		}

		internal string GetDumpMetadataPath(string bundleId, string dumpId) {
			return Path.Combine(GetDumpDirectory(bundleId, dumpId), "dumpinfo.json");
		}
	}
}
