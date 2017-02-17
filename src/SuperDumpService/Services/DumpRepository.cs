using SuperDumpService.Helpers;
using SuperDumpService.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SuperDump.Models;
using System.IO;

namespace SuperDumpService.Services {
	/// <summary>
	/// Stores dump metainfos in memory. Also delegates to persistent storage.
	/// </summary>
	public class DumpRepository {
		private readonly object sync = new object();
		private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, DumpMetainfo>> dumps = new ConcurrentDictionary<string, ConcurrentDictionary<string, DumpMetainfo>>();
		private readonly DumpStorageFilebased storage;
		private readonly BundleRepository bundleRepo;
		private readonly PathHelper pathHelper;

		public DumpRepository(DumpStorageFilebased storage, BundleRepository bundleRepo, PathHelper pathHelper) {
			this.storage = storage;
			this.bundleRepo = bundleRepo;
			this.pathHelper = pathHelper;
		}

		public void Populate() {
			foreach(var bundle in bundleRepo.GetAll()) {
				PopulateForBundle(bundle.BundleId);
			}
		}

		public void PopulateForBundle(string bundleId) {
			lock (sync) {
				dumps.TryAdd(bundleId, new ConcurrentDictionary<string, DumpMetainfo>());
				foreach (var dumpInfo in storage.ReadDumpMetainfoForBundle(bundleId)) {
					dumps[bundleId][dumpInfo.DumpId] = dumpInfo;
				}
			}
		}

		public DumpMetainfo Get(string bundleId, string dumpId) {
			if (!dumps.ContainsKey(bundleId)) return null;
			return dumps[bundleId][dumpId];
		}

		public IEnumerable<DumpMetainfo> Get(string bundleId) {
			if (!dumps.ContainsKey(bundleId)) return null;
			return dumps[bundleId].Values;
		}

		/// <summary>
		/// Adds the actual .dmp file from sourcePath to the repo+storage
		/// Does NOT start analysis
		/// </summary>
		/// <returns></returns>
		public async Task<DumpMetainfo> CreateDump(string bundleId, FileInfo sourcePath) {
			DumpMetainfo dumpInfo;
			string dumpId;
			lock (sync) {
				dumps.TryAdd(bundleId, new ConcurrentDictionary<string, DumpMetainfo>());
				dumpId = CreateUniqueDumpId();
				dumpInfo = new DumpMetainfo() {
					BundleId = bundleId,
					DumpId = dumpId,
					DumpFileName = Utility.MakeRelativePath(pathHelper.GetUploadsDir(), sourcePath),
					DumpType = DetermineDumpType(sourcePath),
					Created = DateTime.Now,
					Status = DumpStatus.Created
				};
				dumps[bundleId][dumpId] = dumpInfo;
			}
			storage.Create(bundleId, dumpId);
			
			FileInfo destFile = await storage.AddFileCopy(bundleId, dumpId, sourcePath);
			AddSDFile(bundleId, dumpId, destFile.Name, SDFileType.PrimaryDump);
			return dumpInfo;
		}

		internal string GetDumpFilePath(string bundleId, string dumpId) {
			return storage.GetDumpFilePath(bundleId, dumpId);
		}

		private DumpType DetermineDumpType(FileInfo sourcePath) {
			if (sourcePath.Name.EndsWith(".dmp", StringComparison.OrdinalIgnoreCase)) return DumpType.WindowsDump;
			if (sourcePath.Name.EndsWith(".core.gz", StringComparison.OrdinalIgnoreCase)) return DumpType.LinuxCoreDump;
			throw new InvalidDataException($"cannot determine dumptype of {sourcePath.FullName}");
		}

		private void AddSDFile(string bundleId, string dumpId, string filename, SDFileType type) {
			var dumpInfo = Get(bundleId, dumpId);
			dumpInfo.Files.Add(new SDFileEntry() {
				FileName = filename,
				Type = type
			});
			storage.Store(dumpInfo);
		}

		public string CreateUniqueDumpId() {
			// create dumpId and make sure it does not exist yet.
			while (true) {
				string dumpId = RandomIdGenerator.GetRandomId();
				if (!dumps.ContainsKey(dumpId)) {
					// does not exist yet. yay.
					return dumpId;
				}
			}
		}

		internal SDResult GetResult(string bundleId, string dumpId) {
			return storage.ReadResults(bundleId, dumpId);
		}

		internal IEnumerable<SDFileInfo> GetFileNames(string bundleId, string dumpId) {
			return storage.GetSDFileInfos(bundleId, dumpId);
		}

		internal void SetDumpStatus(string bundleId, string dumpId, DumpStatus status, string errorMessage = null) {
			var dumpInfo = Get(bundleId, dumpId);
			dumpInfo.Status = status;
			if (!string.IsNullOrEmpty(errorMessage)) {
				dumpInfo.ErrorMessage = errorMessage;
			}
			if (status == DumpStatus.Finished || status == DumpStatus.Failed) {
				dumpInfo.Finished = DateTime.Now;
			}
			storage.Store(dumpInfo);
		}

		internal async Task AddSiblingFile(string bundleId, string dumpId, FileInfo siblingFile) {
			await storage.AddFileCopy(bundleId, dumpId, siblingFile);
			AddSDFile(bundleId, dumpId, siblingFile.Name, SDFileType.SiblingFile);
		}
	}
}
