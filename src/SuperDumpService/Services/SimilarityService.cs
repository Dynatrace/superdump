using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SuperDump.Common;
using SuperDumpService.Helpers;
using SuperDumpService.Models;
using System.Diagnostics;
using SuperDump.Models;

namespace SuperDumpService.Services {
	public class SimilarityService {
		private readonly DumpRepository dumpRepo;

		public SimilarityService(DumpRepository dumpRepo) {
			this.dumpRepo = dumpRepo;
		}

		public void ScheduleSimilarityAnalysis(DumpMetainfo dumpInfo) {
			var result = dumpRepo.GetResult(dumpInfo.BundleId, dumpInfo.DumpId, out string error);

			if (result == null) throw new DumpNotFoundException($"bundleid: {dumpInfo.BundleId}, dumpid: {dumpInfo.DumpId}, no results available");

			// schedule actual analysis
			Hangfire.BackgroundJob.Enqueue<AnalysisService>(repo => CalculateSimilarity(dumpInfo, result));
		}

		[Hangfire.Queue("analysis", Order = 2)]
		public async Task CalculateSimilarity(DumpMetainfo dumpInfo, SDResult result) {
			try {
				var allDumps = dumpRepo.GetAll();
				var similarities = new Dictionary<DumpIdentifier, CrashSimilarity>();
				
				foreach (var dump in allDumps) {
					var otherResult = dumpRepo.GetResult(dumpInfo.BundleId, dumpInfo.DumpId, out string error);

					if (otherResult == null) continue;
					CrashSimilarity crashSimilarity = CrashSimilarity.Calculate(result, otherResult);

					similarities[dump.Id] = crashSimilarity;

					// for every available dump, calculate CrashSimilarity
					// if "OverallSimilarity" is above a certain threshold, store the similarity in a separate datastore into BOTH dumps.
					// the datastore could be just a separate json file within the dump dir. "crashsimilarities.json"

				}
			} catch (Exception e) {
				Console.WriteLine(e.Message);
			} finally {

			}
		}

	}
}
