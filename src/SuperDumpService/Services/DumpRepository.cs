using SuperDumpService.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.Services {
	public class DumpRepository {
		private ConcurrentDictionary<string, ConcurrentDictionary<string, DumpMetainfo>> dumps = new ConcurrentDictionary<string, ConcurrentDictionary<string, DumpMetainfo>>();
		private DumpStorageFilebased storage;
		private BundleRepository bundleRepo;

		public DumpRepository(DumpStorageFilebased storage, BundleRepository bundleRepo) {
			this.storage = storage;
			this.bundleRepo = bundleRepo;
		}

		public void Populate() {
			foreach(var bundle in bundleRepo.GetAll()) {
				PopulateForBundle(bundle.BundleId);
			}
		}

		public void PopulateForBundle(string bundleId) {
			dumps.TryAdd(bundleId, new ConcurrentDictionary<string, DumpMetainfo>());
			foreach (var dumpInfo in storage.ReadDumpMetainfoForBundle(bundleId)) {
				dumps[bundleId][dumpInfo.DumpId] = dumpInfo;
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
	}
}
