using Newtonsoft.Json;
using SuperDumpService.Helpers;
using SuperDumpService.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.Services {
	/// <summary>
	/// for writing and reading of bundles only
	/// this implementation uses simple filebased storage
	/// </summary>
	public class BundleStorageFilebased {
		private readonly DumpStorageFilebased dumpStorage; // use this only for backward compat (populate bundleMetainfo from dumps)

		public BundleStorageFilebased(DumpStorageFilebased dumpStorage) {
			this.dumpStorage = dumpStorage;
		}

		public IEnumerable<BundleMetainfo> ReadBundleMetainfos() {
			foreach (var dir in Directory.EnumerateDirectories(PathHelper.GetWorkingDir())) {
				var bundleId = new DirectoryInfo(dir).Name;
				var metainfoFilename = PathHelper.GetBundleMetadataPath(bundleId);
				if (!File.Exists(metainfoFilename)) {
					// backwards compatibility, when Metadata files did not exist
					CreateBundleMetainfoForCompat(bundleId);

					yield return new BundleMetainfo() { BundleId = bundleId };
				}
				yield return ReadMetainfoFile(metainfoFilename);
			}
		}
		
		private static BundleMetainfo ReadMetainfoFile(string filename) {
			return JsonConvert.DeserializeObject<BundleMetainfo>(File.ReadAllText(filename));
		}

		internal void Store(BundleMetainfo bundleInfo) {
			Directory.CreateDirectory(PathHelper.GetBundleDirectory(bundleInfo.BundleId));
			WriteMetainfoFile(bundleInfo, PathHelper.GetBundleMetadataPath(bundleInfo.BundleId));
		}

		private static void WriteMetainfoFile(BundleMetainfo metaInfo, string filename) {
			File.WriteAllText(filename, JsonConvert.SerializeObject(metaInfo, Formatting.Indented));
		}

		/// <summary>
		/// Older storage did not have metainfo files. We need to read full results, create new metainfo file and store it.
		/// </summary>
		/// <param name="bundleId"></param>
		private void CreateBundleMetainfoForCompat(string bundleId) {
			var metainfo = new BundleMetainfo() {
				BundleId = bundleId
			};
			// use storage directly, not repo. repo might not be initialized yet
			// back then, every dump had the same information encoded. take the first dump and use it from there.

			var dumps = dumpStorage.ReadDumpMetainfoForBundle(bundleId);

			// loop through all dumps and hope to find a good one.
			foreach (var dump in dumps) {
				if (dump != null) {
					metainfo.Created = dump.Created;
					metainfo.Status = BundleStatus.Finished;
					var fullresult = dumpStorage.ReadResults(bundleId, dump.DumpId);
					if (fullresult != null) {
						if (!string.IsNullOrEmpty(fullresult.AnalysisInfo.JiraIssue)) metainfo.CustomProperties["ref"] = fullresult.AnalysisInfo.JiraIssue;
						if (!string.IsNullOrEmpty(fullresult.AnalysisInfo.FriendlyName)) metainfo.CustomProperties["note"] = fullresult.AnalysisInfo.FriendlyName;
						break;
					}
				}
			}
			WriteMetainfoFile(metainfo, PathHelper.GetBundleMetadataPath(bundleId));
		}
	}
}
