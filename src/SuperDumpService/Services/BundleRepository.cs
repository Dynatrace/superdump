using SuperDumpService.Controllers;
using SuperDumpService.Helpers;
using SuperDumpService.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
				if (info == null) continue;
				bundles[info.BundleId] = info;
			}
		}

		public BundleMetainfo Get(string bundleId) {
			return bundles[bundleId];
		}

		public IEnumerable<BundleMetainfo> GetAll() {
			return bundles.Values;
		}

		public BundleMetainfo Create(string filename, DumpAnalysisInput input) {
			lock (sync) {
				string bundleId = CreateUniqueBundleId();
				var bundleInfo = new BundleMetainfo() {
					BundleId = bundleId,
					BundleFileName = filename,
					Created = DateTime.Now,
					Status = BundleStatus.Created
				};
				bundleInfo.CustomProperties = input.CustomProperties;
				if (!string.IsNullOrEmpty(input.JiraIssue)) bundleInfo.CustomProperties["ref"] = input.JiraIssue;
				if (!string.IsNullOrEmpty(input.FriendlyName)) bundleInfo.CustomProperties["note"] = input.FriendlyName;
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

		internal void SetBundleStatus(string bundleId, BundleStatus status, string errorMessage = null) {
			var bundleInfo = Get(bundleId);
			bundleInfo.Status = status;
			if (!string.IsNullOrEmpty(errorMessage)) {
				bundleInfo.ErrorMessage = errorMessage;
			}
			if (status == BundleStatus.Finished || status == BundleStatus.Failed || status == BundleStatus.Duplication) {
				bundleInfo.Finished = DateTime.Now;
			}
			storage.Store(bundleInfo);
		}
	}
}
