using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SuperDump.Common;
using SuperDumpService.Helpers;
using SuperDumpService.Models;
using System.Diagnostics;
using System.Linq;
using SuperDump.Models;
using System.Threading;
using Hangfire.Storage.Monitoring;
using Hangfire.Storage;
using Hangfire;

namespace SuperDumpService.Services {
	public class Relationship {
		public DumpIdentifier DumpA { get; private set; }
		public DumpIdentifier DumpB { get; private set; }
		public CrashSimilarity CrashSimilarity { get; set; }

		public Relationship(DumpIdentifier dumpA, DumpIdentifier dumpB) {
			this.DumpA = dumpA;
			this.DumpB = dumpB;
		}
	}

	public class SimilarityService {
		private readonly DumpRepository dumpRepo;
		private readonly RelationshipRepository relationShipRepo;
		private readonly IOptions<SuperDumpSettings> settings;

		public SimilarityService(DumpRepository dumpRepo, RelationshipRepository relationShipRepository, IOptions<SuperDumpSettings> settings) {
			this.dumpRepo = dumpRepo;
			this.relationShipRepo = relationShipRepository;
			this.settings = settings;
		}

		public async Task<IDictionary<DumpIdentifier, double>> GetSimilarities(DumpIdentifier dumpId) {
			return await relationShipRepo.GetRelationShips(dumpId);
		}

		/// <summary>
		/// if force==true, analysis is run again, even if it's already there.
		/// if force==false, only relationships that have not been analyzed yet are run again.
		/// </summary>
		public void TriggerSimilarityAnalysisForAllDumps(bool force, DateTime timeFrom) {
			if (!settings.Value.SimilarityDetectionEnabled) return;

			Console.WriteLine($"Triggering similarity analysis for all dumps new thatn {timeFrom}. force={force}");
			// start analysis with newest dump
			// for every dump, only analyze newer ones.
			// that way, at first, only newset dumps are compared with newest ones.
			foreach (var dumpInfo in dumpRepo.GetAll().Where(x => x.Created >= timeFrom).OrderByDescending(x => x.Created)) {
				ScheduleSimilarityAnalysis(dumpInfo, force, dumpInfo.Created);
			}
		}

		public void ScheduleSimilarityAnalysis(DumpIdentifier dumpId, bool force, DateTime timeFrom) {
			if (!settings.Value.SimilarityDetectionEnabled) return;

			ScheduleSimilarityAnalysis(dumpRepo.Get(dumpId), force, timeFrom);
		}
		
		public void ScheduleSimilarityAnalysis(DumpMetainfo dumpInfo, bool force, DateTime timeFrom) {
			if (!settings.Value.SimilarityDetectionEnabled) return;

			// schedule actual analysis
			Hangfire.BackgroundJob.Enqueue<SimilarityService>(repo => repo.CalculateSimilarity(dumpInfo, force, timeFrom));
		}

		public void CleanQueue() {
			throw new NotImplementedException();
		}

		public async Task WipeAll() {
			await relationShipRepo.WipeAll();
		}

		public async Task<DumpMiniInfo> GetOrCreateMiniInfo(DumpIdentifier id) {
			if (dumpRepo.MiniInfoExists(id)) {
				try {
					var loadedMiniInfo = await dumpRepo.GetMiniInfo(id);
					if (loadedMiniInfo.DumpSimilarityInfoVersion == CrashSimilarity.MiniInfoVersion) {
						return loadedMiniInfo;
					}
				} catch {
					Console.WriteLine($"could not load miniinfo for {id}. will re-create it.");
				}
			}
			
			// no mini-info exists yet, or version is outdated. re-create.
			var result = await dumpRepo.GetResult(id);
			var miniInfo =
				(result == null)
				? new DumpMiniInfo() // just store an empty mini-info if result is null
				: CrashSimilarity.SDResultToMiniInfo(result);
			await dumpRepo.StoreMiniInfo(id, miniInfo);
			return miniInfo;
		}

		[Hangfire.Queue("similarityanalysis", Order = 3)]
		public void CalculateSimilarity(DumpMetainfo dumpA, bool force, DateTime timeFrom) {
			AsyncHelper.RunSync(() => CalculateSimilarityAsync(dumpA, force, timeFrom));
		}

		public async Task CalculateSimilarityAsync(DumpMetainfo dumpA, bool force, DateTime timeFrom) {
			try {
				var swTotal = new Stopwatch();
				swTotal.Start();
				if (!dumpRepo.IsPopulated) {
					Console.WriteLine($"CalculateSimilarity for {dumpA.Id} is blocked because dumpRepo is not yet fully populated...");
					await Utility.BlockUntil(() => dumpRepo.IsPopulated);
					Console.WriteLine($"...continuing CalculateSimilarity for {dumpA.Id}.");
				}

				var resultA = await GetOrCreateMiniInfo(dumpA.Id);

				var allDumps = dumpRepo.GetAll().Where(x => x.Created >= timeFrom).OrderBy(x => x.Created);
				Console.WriteLine($"starting CalculateSimilarity for {allDumps.Count()} dumps; {dumpA} (TID:{Thread.CurrentThread.ManagedThreadId})");

				var tasks = allDumps.Select(dumpB => Task.Run(async () => {
					if (!force) {
						var existingSimilarity = await relationShipRepo.GetRelationShip(dumpA.Id, dumpB.Id);
						if (existingSimilarity != 0) {
							// relationship already exists. skip!
							// but make sure the relationship is stored bi-directional
							await relationShipRepo.UpdateSimilarity(dumpA.Id, dumpB.Id, existingSimilarity);
							return; 
						}
					}

					if (!PreSelectOnMetadata(dumpA, dumpB)) return;
					var resultB = await GetOrCreateMiniInfo(dumpB.Id);
					if (!PreSelectOnResults(resultA, resultB)) return;

					CrashSimilarity crashSimilarity = CrashSimilarity.Calculate(resultA, resultB);

					// only store value if above a certain threshold to avoid unnecessary disk writes
					if (crashSimilarity.OverallSimilarity > 0.6) {
						await relationShipRepo.UpdateSimilarity(dumpA.Id, dumpB.Id, crashSimilarity.OverallSimilarity);
					}
					//Console.WriteLine($"CalculateSimilarity.Finished for {dumpA}/{dumpB} ({i} to go...); (elapsed: {sw.Elapsed}) (TID:{Thread.CurrentThread.ManagedThreadId})");
				}));
				await Task.WhenAll(tasks);

				await relationShipRepo.FlushDirtyRelationships();
				swTotal.Stop();
				Console.WriteLine($"CalculateSimilarity.Finished for all {allDumps.Count()} dumps (total elapsed: {swTotal.Elapsed}); {dumpA} (TID:{Thread.CurrentThread.ManagedThreadId})");
			} catch (Exception e) {
				Console.WriteLine(e.Message);
			}
		}

		private bool PreSelectOnMetadata(DumpMetainfo dumpA, DumpMetainfo dumpB) {
			return !dumpA.Id.Equals(dumpB.Id) // don't compare the dump to itself
				&& dumpA.Status == DumpStatus.Finished && dumpB.Status == DumpStatus.Finished // both analysis must be finished
				&& dumpA.DumpType == dumpB.DumpType; // don't compare windows and linux dumps
		}

		private bool PreSelectOnResults(in DumpMiniInfo resultA, in DumpMiniInfo resultB) {
			return true; // no ideas on how to pre-select here yet.
		}
	}

	public static class HangfireExtensions {
		public static void PurgeJobs(this IMonitoringApi monitor) {
			var toDelete = new List<string>();

			foreach (QueueWithTopEnqueuedJobsDto queue in monitor.Queues()) {
				for (var i = 0; i < Math.Ceiling(queue.Length / 1000d); i++) {
					monitor.EnqueuedJobs(queue.Name, 1000 * i, 1000)
						.ForEach(x => toDelete.Add(x.Key));
				}
			}

			foreach (var jobId in toDelete) {
				BackgroundJob.Delete(jobId);
			}
		}
	}
}
