using Newtonsoft.Json;
using SuperDumpService.Helpers;
using SuperDumpService.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.Services {
	/// <summary>
	/// for writing and reading of bundles only
	/// this implementation uses simple filebased storage
	/// </summary>
	public class BundleStorageFilebased : IBundleStorage {
		private readonly IDumpStorage dumpStorage; // use this only for backward compat (populate bundleMetainfo from dumps)
		private readonly PathHelper pathHelper;

		public BundleStorageFilebased(IDumpStorage dumpStorage, PathHelper pathHelper) {
			this.dumpStorage = dumpStorage;
			this.pathHelper = pathHelper;
		}

		public async Task<IEnumerable<BundleMetainfo>> ReadBundleMetainfos() {
			var list = new System.Collections.Concurrent.ConcurrentBag<BundleMetainfo>();
			pathHelper.PrepareDirectories();
			var baseDir = new DirectoryInfo(pathHelper.GetWorkingDir());

			var sw = new Stopwatch(); sw.Start();
			var subdirs = baseDir.GetDirectories().OrderByDescending(x => x.CreationTime);
			sw.Stop(); Console.WriteLine($"Getting list of {subdirs.Count()} subdirectories took {sw.Elapsed.TotalSeconds} seconds."); sw.Reset();

			sw.Start();
			var tasks = subdirs.Select(dir => Task.Run(async () => {
				var bundleId = dir.Name;
				var metainfoFilename = pathHelper.GetBundleMetadataPath(bundleId);
				if (!File.Exists(metainfoFilename)) {
					// backwards compatibility, when Metadata files did not exist
					await CreateBundleMetainfoForCompat(bundleId);

					list.Add(new BundleMetainfo() { BundleId = bundleId });
				}
				list.Add(ReadMetainfoFile(metainfoFilename));
			}));
			await Task.WhenAll(tasks);
			sw.Stop(); Console.WriteLine($"ReadBundleMetainfos of {subdirs.Count()} bundles took {sw.Elapsed.TotalSeconds} seconds."); sw.Reset();

			return list;
		}

		private static BundleMetainfo ReadMetainfoFile(string filename) {
			try {
				return JsonConvert.DeserializeObject<BundleMetainfo>(File.ReadAllText(filename));
			} catch (Exception e) {
				Console.Error.WriteLine($"Error reading bundle metadata '{filename}': {e.Message}");
				return null;
			}
		}

		public void Store(BundleMetainfo bundleInfo) {
			Directory.CreateDirectory(pathHelper.GetBundleDirectory(bundleInfo.BundleId));
			WriteMetainfoFile(bundleInfo, pathHelper.GetBundleMetadataPath(bundleInfo.BundleId));
		}

		private static void WriteMetainfoFile(BundleMetainfo metaInfo, string filename) {
			File.WriteAllText(filename, JsonConvert.SerializeObject(metaInfo, Formatting.Indented));
		}

		/// <summary>
		/// Older storage did not have metainfo files. We need to read full results, create new metainfo file and store it.
		/// </summary>
		/// <param name="bundleId"></param>
		private async Task CreateBundleMetainfoForCompat(string bundleId) {
			var metainfo = new BundleMetainfo() {
				BundleId = bundleId
			};
			// use storage directly, not repo. repo might not be initialized yet
			// back then, every dump had the same information encoded. take the first dump and use it from there.

			var dumps = await dumpStorage.ReadDumpMetainfoForBundle(bundleId);

			// loop through all dumps and hope to find a good one.
			foreach (var dump in dumps) {
				if (dump != null) {
					metainfo.Created = dump.Created;
					metainfo.Finished = dump.Created; // can't do better.
					metainfo.Status = BundleStatus.Finished;
					var fullresult = await dumpStorage.ReadResults(dump.Id);
					if (fullresult != null) {
						if (!string.IsNullOrEmpty(fullresult.AnalysisInfo.JiraIssue)) metainfo.CustomProperties["ref"] = fullresult.AnalysisInfo.JiraIssue;
						if (!string.IsNullOrEmpty(fullresult.AnalysisInfo.FriendlyName)) metainfo.CustomProperties["note"] = fullresult.AnalysisInfo.FriendlyName;
						break;
					}
				}
			}
			WriteMetainfoFile(metainfo, pathHelper.GetBundleMetadataPath(bundleId));
		}
	}
}
