using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SuperDumpService.Models;

namespace SuperDumpService.Services.Analyzers {
	public class SimilarityAnalyzerJob : AnalyzerJob {
		private readonly SimilarityService similarityService;
		private readonly IOptions<SuperDumpSettings> settings;

		public SimilarityAnalyzerJob(SimilarityService similarityService, IOptions<SuperDumpSettings> settings) {
			this.similarityService = similarityService;
			this.settings = settings;
		}

		public override Task<AnalyzerState> AnalyzeDump(DumpMetainfo dumpInfo, string analysisWorkingDir, AnalyzerState previousState) {
			if (previousState == AnalyzerState.Success) {
				similarityService.ScheduleSimilarityAnalysis(dumpInfo, false, DateTime.Now - TimeSpan.FromDays(settings.Value.SimilarityDetectionMaxDays));
			}
			return Task.FromResult(previousState);
		}
	}
}
