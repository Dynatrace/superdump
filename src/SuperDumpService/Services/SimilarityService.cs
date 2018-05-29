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

		public SimilarityService(DumpRepository dumpRepo, RelationshipRepository relationShipRepository) {
			this.dumpRepo = dumpRepo;
			this.relationShipRepo = relationShipRepository;
		}

		public IEnumerable<CrashSimilarity> GetSimilarities(DumpIdentifier dumpId) {
			return relationShipRepo.GetRelationShips(dumpId).Select(x => x.CrashSimilarity);
		}

		/// <summary>
		/// if force==true, analysis is run again, even if it's already there.
		/// if force==false, only relationships that have not been analyzed yet are run again.
		/// </summary>
		public void TriggerSimilarityAnalysisForAllDumps(bool force) {
			foreach (var dumpInfo in dumpRepo.GetAll()) {
				ScheduleSimilarityAnalysis(dumpInfo, force);
			}
		}

		public void ScheduleSimilarityAnalysis(DumpIdentifier dumpId, bool force) {
			ScheduleSimilarityAnalysis(dumpRepo.Get(dumpId), force);
		}
		
		public void ScheduleSimilarityAnalysis(DumpMetainfo dumpInfo, bool force) {
			var result = dumpRepo.GetResult(dumpInfo.BundleId, dumpInfo.DumpId, out string error);

			if (result == null) return; // no results found. do nothing.

			// schedule actual analysis
			Hangfire.BackgroundJob.Enqueue<SimilarityService>(repo => repo.CalculateSimilarity(dumpInfo, result, force));
		}


		[Hangfire.Queue("analysis", Order = 2)]
		public async Task CalculateSimilarity(DumpMetainfo dumpA, SDResult resultA, bool force) {
			try {
				var allDumps = dumpRepo.GetAll();

				foreach (var dumpB in allDumps) {
					if (!force) {
						if (relationShipRepo.GetRelationShip(dumpA.Id, dumpB.Id) != null) continue; // relationship already exists. skip!
					}

					var resultB = dumpRepo.GetResult(dumpB.BundleId, dumpB.DumpId, out string error);

					if (resultB == null) continue;
					CrashSimilarity crashSimilarity = CrashSimilarity.Calculate(resultA, resultB);

					// CN: maybe only store if above a certain threshold?
					relationShipRepo.UpdateSimilarity(dumpA.Id, dumpB.Id, crashSimilarity);
				}
			} catch (Exception e) {
				Console.WriteLine(e.Message);
			}
		}

	}
}
