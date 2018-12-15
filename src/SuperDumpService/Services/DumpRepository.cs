using SuperDumpService.Helpers;
using SuperDumpService.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SuperDump.Models;
using System.IO;
using System.Diagnostics;

namespace SuperDumpService.Services {
	/// <summary>
	/// Stores dump metainfos in memory. Also delegates to persistent storage.
	/// </summary>
	public class DumpRepository {
		private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, DumpMetainfo>> dumps = new ConcurrentDictionary<string, ConcurrentDictionary<string, DumpMetainfo>>();
		private readonly ConcurrentDictionary<DumpIdentifier, DumpMiniInfo> miniInfosLazyCache = new ConcurrentDictionary<DumpIdentifier, DumpMiniInfo>();
		private readonly IDumpStorage storage;
		private readonly PathHelper pathHelper;

		public bool IsPopulated { get; private set; }

		public DumpRepository(IDumpStorage storage, PathHelper pathHelper) {
			this.storage = storage;
			this.pathHelper = pathHelper;
		}

		// only BundleRepository is supposed to call this!
		public void SetIsPopulated() {
			IsPopulated = true;
		}

		public async Task PopulateForBundle(string bundleId) {
			var dict = new ConcurrentDictionary<string, DumpMetainfo>();
			foreach (var dumpInfo in await storage.ReadDumpMetainfoForBundle(bundleId)) {
				if (dumpInfo == null) {
					Console.Error.WriteLine($"ReadDumpMetainfoForBundle returned a null entry for bundleId '{bundleId}'");
					continue;
				}
				dict[dumpInfo.DumpId] = dumpInfo;
			}
			dumps.TryAdd(bundleId, dict);
		}

		public DumpMetainfo Get(string bundleId, string dumpId) {
			ConcurrentDictionary<string, DumpMetainfo> bundleInfo = dumps.GetValueOrDefault(bundleId);
			if (bundleInfo != null) {
				return bundleInfo.GetValueOrDefault(dumpId);
			}
			return null;
		}

		public IEnumerable<DumpMetainfo> Get(string bundleId) {
			if (!dumps.ContainsKey(bundleId)) return Enumerable.Empty<DumpMetainfo>();
			return dumps[bundleId].Values;
		}

		public DumpMetainfo Get(DumpIdentifier dumpId) {
			return Get(dumpId.BundleId, dumpId.DumpId);
		}

		public IEnumerable<DumpMetainfo> GetAll() {
			return dumps.SelectMany(bundle => bundle.Value.Values).ToArray();
		}

		/// <summary>
		/// Adds the actual .dmp file from sourcePath to the repo+storage
		/// Does NOT start analysis
		/// </summary>
		/// <returns></returns>
		public async Task<DumpMetainfo> CreateDump(string bundleId, FileInfo sourcePath) {
			DumpMetainfo dumpInfo;
			string dumpId;
			var dict = new ConcurrentDictionary<string, DumpMetainfo>();
			dumpId = CreateUniqueDumpId();
			dumpInfo = new DumpMetainfo() {
				BundleId = bundleId,
				DumpId = dumpId,
				DumpFileName = Utility.MakeRelativePath(pathHelper.GetUploadsDir(), sourcePath),
				DumpType = DetermineDumpType(sourcePath),
				Created = DateTime.Now,
				Status = DumpStatus.Created
			};
			dict[dumpId] = dumpInfo;
			dumps.TryAdd(bundleId, dict);
			storage.Create(bundleId, dumpId);

			FileInfo destFile = await storage.AddFileCopy(bundleId, dumpId, sourcePath);
			AddSDFile(bundleId, dumpId, destFile.Name, SDFileType.PrimaryDump);
			return dumpInfo;
		}

		internal string GetDumpFilePath(DumpIdentifier id) => GetDumpFilePath(id.BundleId, id.DumpId);

		internal string GetDumpFilePath(string bundleId, string dumpId) {
			return storage.GetDumpFilePath(bundleId, dumpId);
		}

		public void ResetDumpTyp(DumpIdentifier id) {
			SetDumpType(id, DetermineDumpType(new FileInfo(Get(id).DumpFileName)));
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

		internal async Task<SDResult> GetResultAndThrow(DumpIdentifier id) => await storage.ReadResultsAndThrow(id.BundleId, id.DumpId);
		internal async Task<SDResult> GetResult(DumpIdentifier id) => await storage.ReadResults(id.BundleId, id.DumpId);

		internal async Task<SDResult> GetResult(string bundleId, string dumpId) {
			return await storage.ReadResults(bundleId, dumpId);
		}

		internal bool MiniInfoExists(DumpIdentifier id) {
			if (miniInfosLazyCache.ContainsKey(id)) return true;
			return storage.MiniInfoExists(id);
		}

		internal async Task<DumpMiniInfo> GetMiniInfo(DumpIdentifier id) {
			if (miniInfosLazyCache.TryGetValue(id, out var cachedMiniInfo)) {
				return cachedMiniInfo;
			}
			var miniInfo = await storage.ReadMiniInfo(id);
			miniInfosLazyCache.TryAdd(id, miniInfo);
			return miniInfo;
		}

		internal async Task StoreMiniInfo(DumpIdentifier id, DumpMiniInfo miniInfo) {
			miniInfosLazyCache.TryAdd(id, miniInfo);
			await storage.StoreMiniInfo(id, miniInfo);
		}

		internal void WriteResult(DumpIdentifier id, SDResult result) {
			storage.WriteResult(id, result);
		}

		internal IEnumerable<SDFileInfo> GetFileNames(string bundleId, string dumpId) {
			return storage.GetSDFileInfos(bundleId, dumpId);
		}

		internal void SetDumpStatus(string bundleId, string dumpId, DumpStatus status, string errorMessage = null) {
			var dumpInfo = Get(bundleId, dumpId);
			dumpInfo.Status = status;
			dumpInfo.ErrorMessage = errorMessage;
			if (status == DumpStatus.Analyzing) {
				dumpInfo.Started = DateTime.Now;
				dumpInfo.Finished = DateTime.MinValue;
			}
			if (status == DumpStatus.Finished || status == DumpStatus.Failed) {
				dumpInfo.Finished = DateTime.Now;
			}
			storage.Store(dumpInfo);
		}

		internal void SetDumpType(DumpIdentifier id, DumpType type) {
			DumpMetainfo dumpInfo = Get(id);
			dumpInfo.DumpType = type;
			storage.Store(dumpInfo);
		}

		internal async Task<FileInfo> AddFileCopy(string bundleId, string dumpId, FileInfo file, SDFileType type) {
			var newFile = await storage.AddFileCopy(bundleId, dumpId, file);
			AddSDFile(bundleId, dumpId, file.Name, type);
			return newFile;
		}

		internal void AddFile(string bundleId, string dumpId, string filename, SDFileType type) {
			AddSDFile(bundleId, dumpId, filename, type);
		}

		public bool IsPrimaryDumpAvailable(string bundleId, string dumpId) {
			return File.Exists(GetDumpFilePath(bundleId, dumpId));
		}
	}
}
