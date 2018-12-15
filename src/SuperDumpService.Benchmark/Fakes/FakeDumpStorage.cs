using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SuperDump.Models;
using SuperDumpService.Models;
using SuperDumpService.Services;

namespace SuperDumpService.Benchmarks.Fakes {
	internal class FakeDump {
		public DumpMetainfo MetaInfo { get; set; }
		public DumpMiniInfo MiniInfo { get; set; }
		public SDResult Result { get; set; }
		public SDFileInfo FileInfo { get; set; }

	}

	internal class FakeDumpStorage : IDumpStorage {
		private readonly int READ_RESULT_DELAY_MS = 10;
		private readonly int READ_MINIINFO_DELAY_MS = 1;
		private readonly int WRITE_MINIINFO_DELAY_MS = 1;
		private readonly int WRITE_DUMPMETAINFO_DELAY_MS = 1;
		private readonly int READ_METAINFO_DELAY_MS = 1;

		private readonly IDictionary<DumpIdentifier, FakeDump> fakeDumpsDict;
		private readonly IDictionary<string, IList<DumpIdentifier>> fakeBundlesDict;

		public FakeDumpStorage(IEnumerable<FakeDump> fakeDumps) {
			this.fakeDumpsDict = new Dictionary<DumpIdentifier, FakeDump>();
			this.fakeBundlesDict = new Dictionary<string, IList<DumpIdentifier>>();
			foreach(var d in fakeDumps) {
				this.fakeDumpsDict[d.MetaInfo.Id] = d;
				if (!this.fakeBundlesDict.ContainsKey(d.MetaInfo.Id.BundleId)) {
					this.fakeBundlesDict[d.MetaInfo.Id.BundleId] = new List<DumpIdentifier>();

				}
				this.fakeBundlesDict[d.MetaInfo.Id.BundleId].Add(d.MetaInfo.Id);
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
			Thread.Sleep(0);
			return fakeDumpsDict.ContainsKey(id) && fakeDumpsDict[id].MiniInfo != null;
		}

		public Task<IEnumerable<DumpMetainfo>> ReadDumpMetainfoForBundle(string bundleId) {
			return Task.FromResult(fakeBundlesDict[bundleId].Select(x => ReadMetainfoFile(x)));
		}

		private DumpMetainfo ReadMetainfoFile(DumpIdentifier id) {
			Thread.Sleep(READ_METAINFO_DELAY_MS);
			return fakeDumpsDict[id].MetaInfo;
		}

		public Task<DumpMiniInfo> ReadMiniInfo(DumpIdentifier id) {
			Thread.Sleep(READ_MINIINFO_DELAY_MS);
			return Task.FromResult(fakeDumpsDict[id].MiniInfo);
		}

		public Task<SDResult> ReadResults(DumpIdentifier id) {
			Thread.Sleep(READ_RESULT_DELAY_MS);
			return Task.FromResult(fakeDumpsDict[id].Result);
		}

		public Task<SDResult> ReadResultsAndThrow(DumpIdentifier id) {
			Thread.Sleep(READ_RESULT_DELAY_MS);
			return Task.FromResult(fakeDumpsDict[id].Result);
		}

		public void Store(DumpMetainfo dumpInfo) {
			Thread.Sleep(WRITE_DUMPMETAINFO_DELAY_MS);
		}

		public Task StoreMiniInfo(DumpIdentifier id, DumpMiniInfo miniInfo) {
			Thread.Sleep(WRITE_MINIINFO_DELAY_MS);
			fakeDumpsDict[id].MiniInfo = miniInfo;
			return Task.Delay(0);
		}

		public void WriteResult(DumpIdentifier id, SDResult result) {
			Thread.Sleep(READ_RESULT_DELAY_MS);
		}
	}
}