using System;
using System.IO;

namespace SuperDumpService.Helpers {
	public static class PathHelper {
		private static string workingDir = Path.Combine(Directory.GetCurrentDirectory(), @"../../data/dumps/");
		private static string uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), @"../../data/uploads/");
		private static string hangfireDbDir = Path.Combine(Directory.GetCurrentDirectory(), @"../../data/hangfire/");
		private static string confDir = Path.Combine(Directory.GetCurrentDirectory(), @"../../conf/");

		internal static string GetHangfireDBDir() {
			return hangfireDbDir;
		}

		public static string GetUploadsDir() {
			return Path.GetFullPath(uploadsDir);
		}

		public static string GetWorkingDir() {
			return Path.GetFullPath(workingDir);
		}

		public static string GetDumpDirectory(string bundleId, string dumpId) {
			return Path.Combine(GetBundleDirectory(bundleId), dumpId);
		}

		internal static string GetConfDirectory() {
			return confDir;
		}

		public static string GetBundleDirectory(string bundleId) {
			return Path.Combine(GetWorkingDir(), bundleId);
		}

		public static string GetBundleDownloadPath(string fileName) {
			return Path.Combine(GetUploadsDir(), fileName);
		}

		/// <summary>
		/// makes sure all required directories exist
		/// </summary>
		internal static void PrepareDirectories() {
			Directory.CreateDirectory(GetWorkingDir());
			Directory.CreateDirectory(GetUploadsDir());
			Directory.CreateDirectory(GetHangfireDBDir());
		}

		public static string GetDumpfilePath(string bundleId, string dumpId) {
			return Path.Combine(GetDumpDirectory(bundleId, dumpId), dumpId + ".dmp");
		}

		public static string GetJsonPath(string bundleId, string dumpId) {
			return Path.Combine(GetDumpDirectory(bundleId, dumpId), dumpId + ".json");
		}

		public static string GetDumpSelectorPath() {
			string dumpselector = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\SuperDumpSelector\bin\SuperDumpSelector.exe"));
			if (!File.Exists(dumpselector)) {
				// deployment case
				dumpselector = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\SuperDumpSelector\SuperDumpSelector.exe"));
			}
			return dumpselector;
		}

		internal static string GetBundleMetadataPath(string bundleId) {
			return Path.Combine(GetBundleDirectory(bundleId), "bundleinfo.json");
		}

		internal static string GetDumpMetadataPath(string bundleId, string dumpId) {
			return Path.Combine(GetDumpDirectory(bundleId, dumpId), "dumpinfo.json");
		}
	}
}
