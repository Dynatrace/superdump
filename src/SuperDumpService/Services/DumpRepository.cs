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
using Microsoft.Extensions.Options;

namespace SuperDumpService.Services {
	/// <summary>
	/// Stores dump metainfos in memory. Also delegates to persistent storage.
	/// </summary>
	public class DumpRepository {
		private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, DumpMetainfo>> dumps = new ConcurrentDictionary<string, ConcurrentDictionary<string, DumpMetainfo>>();
		private readonly ConcurrentDictionary<DumpIdentifier, DumpMiniInfo> miniInfosLazyCache = new ConcurrentDictionary<DumpIdentifier, DumpMiniInfo>();
		private readonly IDumpStorage storage;
		private readonly PathHelper pathHelper;
		private readonly SuperDumpSettings settings;

		public bool IsPopulated { get; private set; }

		public DumpRepository(IDumpStorage storage, PathHelper pathHelper, IOptions<SuperDumpSettings> settings) {
			this.storage = storage;
			this.pathHelper = pathHelper;
			this.settings = settings.Value;
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

		public DumpMetainfo Get(DumpIdentifier id) {
			ConcurrentDictionary<string, DumpMetainfo> bundleInfo = dumps.GetValueOrDefault(id.BundleId);
			if (bundleInfo != null) {
				return bundleInfo.GetValueOrDefault(id.DumpId);
			}
			return null;
		}

		public IEnumerable<DumpMetainfo> Get(string bundleId) {
			if (!dumps.ContainsKey(bundleId)) return Enumerable.Empty<DumpMetainfo>();
			return dumps[bundleId].Values;
		}

		public IEnumerable<DumpMetainfo> GetAll() {
			return dumps.SelectMany(bundle => bundle.Value.Values).ToArray();
		}

		/// <summary>
		/// Adds the actual .dmp file from sourcePath to the repo+storage
		/// Does NOT start analysis
		/// </summary>
		/// <returns></returns>
		public async Task<DumpMetainfo> CreateDump(string bundleId, FileInfo sourcePath, DumpType dumpType) {
			DumpMetainfo dumpInfo = CreateDumpMetainfo(bundleId);

			dumpInfo.DumpFileName = Utility.MakeRelativePath(pathHelper.GetUploadsDir(), sourcePath);
			dumpInfo.DumpType = dumpType;

			FileInfo destFile = await storage.AddFileCopy(dumpInfo.Id, sourcePath);
			AddSDFile(dumpInfo.Id, destFile.Name, SDFileType.PrimaryDump);
			return dumpInfo;
		}

		public DumpMetainfo CreateDumpMetainfo(string bundleId) {
			DumpMetainfo dumpInfo;
			string dumpId;
			dumpId = CreateUniqueDumpId();
			dumpInfo = new DumpMetainfo() {
				BundleId = bundleId,
				DumpId = dumpId,
				Created = DateTime.Now,
				Status = DumpStatus.Created
			};
			if (settings.IsDumpRetentionEnabled()) {
				dumpInfo.PlannedDeletionDate = DateTime.Now + TimeSpan.FromDays(settings.DumpRetentionDays);
			}

			if (!dumps.ContainsKey(bundleId)) dumps[bundleId] = new ConcurrentDictionary<string, DumpMetainfo>();
			dumps[bundleId][dumpId] = dumpInfo;
			storage.Create(dumpInfo.Id);

			return dumpInfo;
		}

		public DumpMetainfo CreateEmptyDump(string bundleId) {
			DumpMetainfo dumpInfo = CreateDumpMetainfo(bundleId);
			dumpInfo.DumpType = DumpType.Empty;
			dumpInfo.IsPrimaryDumpAvailable = false;
			storage.Store(dumpInfo);
			return dumpInfo;
		}

		public void UpdateIsDumpAvailable(DumpIdentifier id) {
			DumpMetainfo dumpMetainfo = Get(id);
			dumpMetainfo.IsPrimaryDumpAvailable = storage.ReadIsPrimaryDumpAvailable(dumpMetainfo);
		}

		public string GetDumpFilePath(DumpIdentifier id) {
			return storage.GetDumpFilePath(id);
		}

		public void ResetDumpTyp(DumpIdentifier id) {
			string filename = Get(id).DumpFileName;
			if (filename.EndsWith(".dmp", StringComparison.OrdinalIgnoreCase)) SetDumpType(id, DumpType.WindowsDump);
			if (filename.EndsWith(".core.gz", StringComparison.OrdinalIgnoreCase)) SetDumpType(id, DumpType.LinuxCoreDump);
			if (filename.EndsWith(".core", StringComparison.OrdinalIgnoreCase)) SetDumpType(id, DumpType.LinuxCoreDump);
		}

		public void DeleteDumpFile(DumpIdentifier id) {
			storage.DeleteDumpFile(id);
		}

		private void AddSDFile(DumpIdentifier id, string filename, SDFileType type) {
			var dumpInfo = Get(id);
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

		public async Task<SDResult> GetResultAndThrow(DumpIdentifier id) => await storage.ReadResultsAndThrow(id);
		public async Task<SDResult> GetResult(DumpIdentifier id) => await storage.ReadResults(id);

		public bool MiniInfoExists(DumpIdentifier id) {
			if (miniInfosLazyCache.ContainsKey(id)) return true;
			return storage.MiniInfoExists(id);
		}

		public async Task<DumpMiniInfo> GetMiniInfo(DumpIdentifier id) {
			if (miniInfosLazyCache.TryGetValue(id, out var cachedMiniInfo)) {
				return cachedMiniInfo;
			}
			var miniInfo = await storage.ReadMiniInfo(id);
			miniInfosLazyCache.TryAdd(id, miniInfo);
			return miniInfo;
		}

		public async Task StoreMiniInfo(DumpIdentifier id, DumpMiniInfo miniInfo) {
			miniInfosLazyCache.TryAdd(id, miniInfo);
			await storage.StoreMiniInfo(id, miniInfo);
		}

		public void WriteResult(DumpIdentifier id, SDResult result) {
			storage.WriteResult(id, result);
		}

		public IEnumerable<SDFileInfo> GetFileNames(DumpIdentifier id) {
			return storage.GetSDFileInfos(id);
		}

		public void SetDumpStatus(DumpIdentifier id, DumpStatus status, string errorMessage = null) {
			var dumpInfo = Get(id);
			dumpInfo.Status = status;
			if (errorMessage != null) {
				dumpInfo.ErrorMessage = errorMessage;
			}
			if (status == DumpStatus.Analyzing) {
				dumpInfo.Started = DateTime.Now;
				dumpInfo.Finished = DateTime.MinValue;
			}
			if (status == DumpStatus.Finished || status == DumpStatus.Failed) {
				dumpInfo.Finished = DateTime.Now;
			}
			storage.Store(dumpInfo);
		}

		public void SetErrorMessage(DumpIdentifier id, string errorMessage) {
			var dumpInfo = Get(id);
			dumpInfo.ErrorMessage = errorMessage;
			storage.Store(dumpInfo);
		}

		public void SetDumpType(DumpIdentifier id, DumpType type) {
			DumpMetainfo dumpInfo = Get(id);
			dumpInfo.DumpType = type;
			storage.Store(dumpInfo);
		}

		public async Task<FileInfo> AddFileCopy(DumpIdentifier id, FileInfo file, SDFileType type) {
			var newFile = await storage.AddFileCopy(id, file);
			AddSDFile(id, file.Name, type);
			return newFile;
		}

		public void AddFile(DumpIdentifier id, string filename, SDFileType type) {
			AddSDFile(id, filename, type);
		}

		public bool IsPrimaryDumpAvailable(DumpIdentifier id) {
			return Get(id)?.IsPrimaryDumpAvailable ?? false;
		}

		public void SetPlannedDeletionDate(DumpIdentifier id, DateTime plannedDeletionDate, string reason) {
			DumpMetainfo dumpInfo = Get(id);
			dumpInfo.PlannedDeletionDate = plannedDeletionDate;
			dumpInfo.RetentionTimeExtensionReason = reason;
			storage.Store(dumpInfo);
		}
	}
}
