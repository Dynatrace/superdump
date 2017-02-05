using SuperDumpService.Helpers;
using SuperDumpService.Models;
using SuperDumpService.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.Services {
	/// <summary>
	/// Stores bundle metainfos in memory. Also delegates to persistent storage.
	/// </summary>
	public class BundleRepository {
		private readonly object sync = new object();
		private readonly ConcurrentDictionary<string, BundleMetainfo> bundles = new ConcurrentDictionary<string, BundleMetainfo>();
		private readonly BundleStorageFilebased storage;

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

		public IEnumerable<BundleMetainfo> GetAll() {
			return bundles.Values;
		}

		public BundleMetainfo Create() {
			lock (sync) {
				string bundleId = CreateUniqueBundleId();
				var bundleInfo = new BundleMetainfo() {
					BundleId = bundleId,
					Created = DateTime.Now,
					Status = BundleStatus.Created
				};
				bundles[bundleId] = bundleInfo;
				storage.Store(bundleInfo);
				return bundleInfo;
			}
		}

		public string CreateUniqueBundleId() {
			// create bundleId and make sure it does not exist yet.
			while (true) {
				string bundleId = RandomIdGenerator.GetRandomId();
				if (!ContainsBundle(bundleId)) {
					// does not exist yet. yay.
					return bundleId;
				}
			}
		}

		public bool ContainsBundle(string bundleId) {
			return bundles.ContainsKey(bundleId);
		}

		internal void SetBundleStatus(string bundleId, BundleStatus status) {
			var bundleInfo = Get(bundleId);
			bundleInfo.Status = status;
			storage.Store(bundleInfo);
		}
	}
}
