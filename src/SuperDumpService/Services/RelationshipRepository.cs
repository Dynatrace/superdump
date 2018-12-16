using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SuperDumpService.Helpers;
using SuperDumpService.Models;

namespace SuperDumpService.Services {

	public class RelationshipRepository {
		private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

		// stores relationships bi-directional. both directions should stay in sync
		private readonly ConcurrentDictionary<DumpIdentifier, IDictionary<DumpIdentifier, double>> relationShips = new ConcurrentDictionary<DumpIdentifier, IDictionary<DumpIdentifier, double>>();
		private readonly IRelationshipStorage relationshipStorage;
		private readonly DumpRepository dumpRepo;
		private readonly IOptions<SuperDumpSettings> settings;
		public bool IsPopulated { get; private set; } = false;

		private readonly HashSet<DumpIdentifier> dirtyDumps; // list of dumps that have updated relationships that are not written to disk yet
		private readonly SemaphoreSlim flushSync = new SemaphoreSlim(1, 1);

		public RelationshipRepository(IRelationshipStorage relationshipStorage, DumpRepository dumpRepo, IOptions<SuperDumpSettings> settings) {
			this.relationshipStorage = relationshipStorage;
			this.dumpRepo = dumpRepo;
			this.settings = settings;
			this.dirtyDumps = new HashSet<DumpIdentifier>();
		}

		public async Task Populate() {
			if (!settings.Value.SimilarityDetectionEnabled) return;
			await BlockIfBundleRepoNotReady("RelationshipRepository.Populate");

			await semaphoreSlim.WaitAsync().ConfigureAwait(false);
			var sw = new Stopwatch(); sw.Start();
			try {
				var tasks = dumpRepo.GetAll().Select(dump => Task.Run(async () => {
					try {
						relationShips[dump.Id] = await relationshipStorage.ReadRelationships(dump.Id);
					} catch (FileNotFoundException) {
						// ignore.
					} catch (Exception e) {
						Console.WriteLine($"RelationshipRepository.Populate: Error reading relationship file for dump {dump.Id}: " + e.Message);
						relationshipStorage.Wipe(dump.Id);
					}
				}));
				await Task.WhenAll(tasks);
			} finally {
				IsPopulated = true;
				semaphoreSlim.Release();
			}
			sw.Stop(); Console.WriteLine($"Finished populating RelationshipRepository in {sw.Elapsed}");
		}

		/// <summary>
		/// updates similarity between two dumps (for both)
		/// important: changes are not written to storage immediately.
		///            call FlushDirtyRelationships to write relationships to storage!
		/// </summary>
		public async Task UpdateSimilarity(DumpIdentifier dumpA, DumpIdentifier dumpB, double similarity) {
			if (!settings.Value.SimilarityDetectionEnabled) return;

			await semaphoreSlim.WaitAsync().ConfigureAwait(false);
			try {
				UpdateSimilarity0(dumpA, dumpB, similarity);
				UpdateSimilarity0(dumpB, dumpA, similarity);
			} finally {
				semaphoreSlim.Release();
			}
		}

		private void UpdateSimilarity0(DumpIdentifier dumpA, DumpIdentifier dumpB, double similarity) {
			if (relationShips.TryGetValue(dumpA, out IDictionary<DumpIdentifier, double> relationShip)) {
				relationShip[dumpB] = similarity;
			} else {
				var dict = new Dictionary<DumpIdentifier, double>();
				dict[dumpB] = similarity;
				relationShips[dumpA] = dict;
			}

			// don't write to storage immediately. only mark it dirty.
			MarkRelationshipDirty(dumpA);
		}

		/// <summary>
		/// 
		/// </summary>
		private void MarkRelationshipDirty(DumpIdentifier dumpA) {
			lock (dirtyDumps) {
				dirtyDumps.Add(dumpA);
			}
		}

		/// <summary>
		/// Write all relationships to disk that are marked as dirty
		/// </summary>
		public async Task FlushDirtyRelationships() {
			// make sure only one flush can happen at the time

			await flushSync.WaitAsync().ConfigureAwait(false);
			try {
				DumpIdentifier[] dirtyDumpsCopy;
				lock (dirtyDumps) {
					// copy list while synchonized
					dirtyDumpsCopy = new DumpIdentifier[dirtyDumps.Count];
					dirtyDumps.CopyTo(dirtyDumpsCopy, 0);
					dirtyDumps.Clear();
				}

				// write all dirty relationships via tasks (in parallel)
				var tasks = dirtyDumpsCopy.Select(id => Task.Run(async () => {
					await relationshipStorage.StoreRelationships(id, await GetRelationShips(id));
				}));
				await Task.WhenAll(tasks);
			} finally {
				flushSync.Release();
			}
		}

		public async Task WipeAll() {
			await semaphoreSlim.WaitAsync().ConfigureAwait(false);
			try {
				foreach (var item in relationShips) {
					relationshipStorage.Wipe(item.Key);
				}
				relationShips.Clear();
			} finally {
				semaphoreSlim.Release();
			}
		}

		public async Task<double> GetRelationShip(DumpIdentifier dumpA, DumpIdentifier dumpB) {
			await semaphoreSlim.WaitAsync().ConfigureAwait(false);
			try {
				if (relationShips.TryGetValue(dumpA, out IDictionary<DumpIdentifier, double> relationShips1)) {
					if (relationShips1.TryGetValue(dumpB, out double rel1)) return rel1;
				}
				if (relationShips.TryGetValue(dumpB, out IDictionary<DumpIdentifier, double> relationShips2)) {
					if (relationShips2.TryGetValue(dumpA, out double rel2)) return rel2;
				}
				return 0;
			} finally {
				semaphoreSlim.Release();
			}
		}

		public async Task<IDictionary<DumpIdentifier, double>> GetRelationShips(DumpIdentifier dumpA) {
			await semaphoreSlim.WaitAsync().ConfigureAwait(false);
			try {
				if (relationShips.TryGetValue(dumpA, out IDictionary<DumpIdentifier, double> relationShips1)) {
					return relationShips1;
				}
				return new Dictionary<DumpIdentifier, double>();
			} finally {
				semaphoreSlim.Release();
			}
		}

		/// <summary>
		/// Blocks until bundleRepo is fully populated.
		/// </summary>
		private async Task BlockIfBundleRepoNotReady(string sourcemethod) {
			if (!dumpRepo.IsPopulated) {
				Console.WriteLine($"{sourcemethod} is blocked because dumpRepo is not yet fully populated...");
				await Utility.BlockUntil(() => dumpRepo.IsPopulated);
				Console.WriteLine($"...continuing {sourcemethod}.");
			}
		}
	}
}
