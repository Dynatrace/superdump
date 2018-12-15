using Microsoft.Extensions.Options;
using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using SuperDumpService.Models;

namespace SuperDumpService.Helpers {
	public class PathHelper {
		private readonly string workingDir;
		private readonly string uploadsDir;
		private readonly string hangfireDbDir;
		private static string confDir = Path.Combine(Directory.GetCurrentDirectory(), @"../../conf/");
		private static string confDirFallback = Directory.GetCurrentDirectory();

		public PathHelper(string workingDir, string uploadsDir, string hangfireDbDir) {
			// maybe there is a smarter way to convert IConfigurationSection to SuperDumpSettings?
			this.workingDir = workingDir;
			this.uploadsDir = uploadsDir;
			this.hangfireDbDir = hangfireDbDir;
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

		public string GetDumpDirectory(DumpIdentifier id) {
			return Path.Combine(GetBundleDirectory(id.BundleId), id.DumpId);
		}

		internal string GetDumpMiniInfoPath(DumpIdentifier id) {
			return Path.Combine(GetDumpDirectory(id), "mini-info.json");
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

		public string GetJsonPath(DumpIdentifier id) {
			return Path.Combine(GetDumpDirectory(id), "superdump-result.json");
		}

		internal string GetJsonPathFallback(DumpIdentifier id) {
			return Path.Combine(GetDumpDirectory(id), id.DumpId + ".json");
		}

		internal string GetBundleMetadataPath(string bundleId) {
			return Path.Combine(GetBundleDirectory(bundleId), "bundleinfo.json");
		}

		internal string GetDumpMetadataPath(DumpIdentifier id) {
			return Path.Combine(GetDumpDirectory(id), "dumpinfo.json");
		}

		internal string GetRelationshipsPath(DumpIdentifier id) {
			return Path.Combine(GetDumpDirectory(id), "relationships.json");
		}

		internal string GetIdenticRelationshipsPath(string bundleId) {
			return Path.Combine(GetBundleDirectory(bundleId), "identic-relationships.json");
		}

		internal string GetJiraIssuePath(string bundleId) {
			return Path.Combine(GetBundleDirectory(bundleId), "jira-issues.json");
		}
	}
}
