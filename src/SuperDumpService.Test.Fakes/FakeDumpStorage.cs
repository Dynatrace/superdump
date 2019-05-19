using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SuperDump.Models;
using SuperDumpService.Models;
using SuperDumpService.Services;

namespace SuperDumpService.Test.Fakes {
	public class FakeDump {
		public DumpMetainfo MetaInfo { get; set; }
		public DumpMiniInfo? MiniInfo { get; set; }
		public SDResult Result { get; set; }
		public SDFileInfo FileInfo { get; set; }
	}

	public class FakeBundle {
		public BundleMetainfo MetaInfo { get; set; }
		public IList<DumpIdentifier> Dumps { get; set; } = new List<DumpIdentifier>();
	}

	public class FakeDumpStorage : IDumpStorage, IBundleStorage {
		private readonly int READ_RESULT_DELAY_MS = 1;
		private readonly int READ_MINIINFO_DELAY_MS = 1;
		private readonly int WRITE_MINIINFO_DELAY_MS = 1;
		private readonly int WRITE_DUMPMETAINFO_DELAY_MS = 1;
		private readonly int READ_METAINFO_DELAY_MS = 1;

		private readonly IDictionary<DumpIdentifier, FakeDump> fakeDumpsDict;
		private readonly IDictionary<string, FakeBundle> fakeBundlesDict;

		public bool DelaysEnabled { get; set; }

		public FakeDumpStorage() : this(Enumerable.Empty<FakeDump>()) { }

		public FakeDumpStorage(IEnumerable<FakeDump> fakeDumps) {
			this.fakeDumpsDict = new Dictionary<DumpIdentifier, FakeDump>();
			this.fakeBundlesDict = new Dictionary<string, FakeBundle>();
			foreach(var d in fakeDumps) {
				this.fakeDumpsDict[d.MetaInfo.Id] = d;
				if (!this.fakeBundlesDict.ContainsKey(d.MetaInfo.Id.BundleId)) {
					this.fakeBundlesDict[d.MetaInfo.Id.BundleId] = new FakeBundle {
						MetaInfo = new BundleMetainfo {
							BundleId = d.MetaInfo.BundleId
						}
					};
				}
				this.fakeBundlesDict[d.MetaInfo.Id.BundleId].Dumps.Add(d.MetaInfo.Id);
			}
		}

		public Task<FileInfo> AddFileCopy(DumpIdentifier id, FileInfo sourcePath) {
			throw new NotImplementedException();
		}

		public void Create(DumpIdentifier id) {
			throw new NotImplementedException();
		}

		public void DeleteDumpFile(DumpIdentifier id) {
			throw new NotImplementedException();
		}

		public string GetDumpFilePath(DumpIdentifier id) {
			throw new NotImplementedException();
		}

		public FileInfo GetFile(DumpIdentifier id, string filename) {
			throw new NotImplementedException();
		}

		public IEnumerable<SDFileInfo> GetSDFileInfos(DumpIdentifier id) {
			throw new NotImplementedException();
		}

		public bool MiniInfoExists(DumpIdentifier id) {
			return fakeDumpsDict.ContainsKey(id) && fakeDumpsDict[id].MiniInfo != null;
		}

		public async Task<IEnumerable<DumpMetainfo>> ReadDumpMetainfoForBundle(string bundleId) {
			return await Task.FromResult(fakeBundlesDict[bundleId].Dumps.Select(x => ReadMetainfoFile(x)));
		}

		private DumpMetainfo ReadMetainfoFile(DumpIdentifier id) {
			if (DelaysEnabled) Thread.Sleep(READ_METAINFO_DELAY_MS);
			return fakeDumpsDict[id].MetaInfo;
		}

		public async Task<DumpMiniInfo> ReadMiniInfo(DumpIdentifier id) {
			if (DelaysEnabled) await Task.Delay(READ_MINIINFO_DELAY_MS);
			return await Task.FromResult(fakeDumpsDict[id].MiniInfo.Value);
		}

		public async Task<SDResult> ReadResults(DumpIdentifier id) {
			if (DelaysEnabled) await Task.Delay(READ_RESULT_DELAY_MS);
			return await Task.FromResult(fakeDumpsDict[id].Result);
		}

		public async Task<SDResult> ReadResultsAndThrow(DumpIdentifier id) {
			if (DelaysEnabled) await Task.Delay(READ_RESULT_DELAY_MS);
			return await Task.FromResult(fakeDumpsDict[id].Result);
		}

		public void Store(DumpMetainfo dumpInfo) {
			if (DelaysEnabled) Thread.Sleep(WRITE_DUMPMETAINFO_DELAY_MS);
		}

		public async Task StoreMiniInfo(DumpIdentifier id, DumpMiniInfo miniInfo) {
			if (DelaysEnabled) await Task.Delay(WRITE_MINIINFO_DELAY_MS);
			fakeDumpsDict[id].MiniInfo = miniInfo;
		}

		public void WriteResult(DumpIdentifier id, SDResult result) {
			if (DelaysEnabled) Thread.Sleep(READ_RESULT_DELAY_MS);
		}

		public Task<IEnumerable<BundleMetainfo>> ReadBundleMetainfos() {
			return Task.FromResult(fakeBundlesDict.Select(x => x.Value.MetaInfo));
		}

		public void Store(BundleMetainfo bundleInfo) {
			throw new NotImplementedException();
		}

		public bool ReadIsPrimaryDumpAvailable(DumpIdentifier id) {
			throw new NotImplementedException();
		}
	}
}