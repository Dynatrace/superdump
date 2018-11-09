using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SuperDumpService.Helpers;
using SuperDumpService.Models;

namespace SuperDumpService.Services {
	public class IdenticalDumpRepository {
		private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

		private readonly IDictionary<string, HashSet<string>> identicalDumps = new Dictionary<string, HashSet<string>>();
		private readonly IdenticalDumpStorageFilebased identicalDumpStorage;
		private readonly BundleRepository bundleRepo;

		public IdenticalDumpRepository(IdenticalDumpStorageFilebased identicalDumpStorage, BundleRepository bundleRepo) {
			this.identicalDumpStorage = identicalDumpStorage;
			this.bundleRepo = bundleRepo;
		}

		public async Task Populate() {
			await BlockIfBundleRepoNotReady("IdenticalDumpRepository.Populate");

			await semaphoreSlim.WaitAsync().ConfigureAwait(false);
			try {
				foreach (BundleMetainfo bundle in bundleRepo.GetAll()) {
					try {
						HashSet<string> identicals = await identicalDumpStorage.Read(bundle.BundleId);
						if (identicals != null) {
							identicalDumps[bundle.BundleId] = identicals;
						}
					} catch (Exception e) {
						Console.WriteLine("error reading identical-dump file: " + e.ToString());
						identicalDumpStorage.Wipe(bundle.BundleId);
					}
				}
			} finally {
				semaphoreSlim.Release();
			}
		}

		public async Task CreateAllIdenticalRelationships() {
			await BlockIfBundleRepoNotReady("IdenticalDumpRepository.CreateAllIdenticalRelationships");

			await semaphoreSlim.WaitAsync().ConfigureAwait(false);
			try {
				foreach (BundleMetainfo bundleInfo in bundleRepo.GetAll().Where(bundleInfo => bundleInfo.Status == BundleStatus.Duplication)) {
					await identicalDumpStorage.Store(bundleInfo.OriginalBundleId, bundleInfo.BundleId);
					AddToListInDict(bundleInfo.OriginalBundleId, bundleInfo.BundleId);
				}
			} finally {
				semaphoreSlim.Release();
			}
		}

		private void AddToListInDict(string originalBundleId, string identicalBundleId) {
			if (identicalDumps.TryGetValue(originalBundleId, out HashSet<string> identicals)) {
				identicals.Add(identicalBundleId);
			} else {
				identicalDumps[originalBundleId] = new HashSet<string>() { identicalBundleId };
			}
		}

		public async Task AddIdenticalRelationship(string originalBundleId, string identicalBundleId) {
			await semaphoreSlim.WaitAsync().ConfigureAwait(false);
			try {
				await identicalDumpStorage.Store(originalBundleId, identicalBundleId);
				AddToListInDict(originalBundleId, identicalBundleId);
			} finally {
				semaphoreSlim.Release();
			}
		}

		public async Task WipeAll() {
			await semaphoreSlim.WaitAsync().ConfigureAwait(false);
			try {
				foreach (var item in identicalDumps) {
					identicalDumpStorage.Wipe(item.Key);
				}
				identicalDumps.Clear();
			} finally {
				semaphoreSlim.Release();
			}
		}

		public async Task<IEnumerable<string>> GetIdenticalRelationships(string bundleId) {
			await semaphoreSlim.WaitAsync().ConfigureAwait(false);
			try {
				if (identicalDumps.TryGetValue(bundleId, out HashSet<string> relationShips)) {
					return relationShips;
				}
				return Enumerable.Empty<string>();
			} finally {
				semaphoreSlim.Release();
			}
		}

		/// <summary>
		/// Blocks until bundleRepo is fully populated.
		/// </summary>
		private async Task BlockIfBundleRepoNotReady(string sourcemethod) {
			if (!bundleRepo.IsPopulated) {
				Console.WriteLine($"{sourcemethod} is blocked because dumpRepo is not yet fully populated...");
				await Utility.BlockUntil(() => bundleRepo.IsPopulated);
				Console.WriteLine($"...continuing {sourcemethod}.");
			}
		}
	}
}
