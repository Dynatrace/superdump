using SuperDumpService.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.Services {
	public class BundleRepository {
		private ConcurrentDictionary<string, BundleMetainfo> bundles = new ConcurrentDictionary<string, BundleMetainfo>();
		private BundleStorageFilebased storage;

		public BundleRepository(BundleStorageFilebased storage) {
			this.storage = storage;
		}

		public void Populate() {
			foreach(var info in storage.ReadBundleMetainfos()) {
				bundles[info.BundleId] = info;
			}
		}

		public BundleMetainfo Get(string bundleId) {
			return bundles[bundleId];
		}

		internal IEnumerable<BundleMetainfo> GetAll() {
			return bundles.Values;
		}
	}
}
