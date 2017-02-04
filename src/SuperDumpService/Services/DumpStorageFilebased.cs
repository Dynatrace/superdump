using Newtonsoft.Json;
using SuperDump.Models;
using SuperDumpService.Helpers;
using SuperDumpService.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.Services {
	/// <summary>
	/// for writing and reading of dumps only
	/// this implementation uses simple filebased storage
	/// </summary>
	public class DumpStorageFilebased {

		public DumpStorageFilebased() {

		}

		public IEnumerable<DumpMetainfo> ReadDumpMetainfoForBundle(string bundleId) {
			foreach (var dir in Directory.EnumerateDirectories(PathHelper.GetBundleDirectory(bundleId))) {
				var dumpId = new DirectoryInfo(dir).Name;
				var metainfoFilename = PathHelper.GetDumpMetadataPath(bundleId, dumpId);
				if (!File.Exists(metainfoFilename)) {
					// backwards compatibility, when Metadata files did not exist. read full json, then store metadata file
					CreateMetainfoForCompat(bundleId, dumpId);
				}
				yield return ReadMetainfoFile(metainfoFilename);
			}
		}

		private static DumpMetainfo ReadMetainfoFile(string filename) {
			return JsonConvert.DeserializeObject<DumpMetainfo>(File.ReadAllText(filename));
		}

		private static void WriteMetainfoFile(DumpMetainfo metaInfo, string filename) {
			File.WriteAllText(filename, JsonConvert.SerializeObject(metaInfo));
		}

		private void CreateMetainfoForCompat(string bundleId, string dumpId) {
			var metainfo = new DumpMetainfo() {
				BundleId = bundleId,
				DumpId = dumpId
			};
			var result = ReadFullResult(bundleId, dumpId);
			if (result != null) {
				metainfo.Status = DumpStatus.Finished;
				metainfo.Created = result.AnalysisInfo.ServerTimeStamp;
			} else {
				metainfo.Status = DumpStatus.Failed;
			}
			
			WriteMetainfoFile(metainfo, PathHelper.GetDumpMetadataPath(bundleId, dumpId));
		}

		public SDResult ReadFullResult(string bundleId, string dumpId) {
			var filename = PathHelper.GetJsonPath(bundleId, dumpId);
			if (!File.Exists(filename)) return null;
			return JsonConvert.DeserializeObject<SDResult>(File.ReadAllText(filename));
		}
	}
}
