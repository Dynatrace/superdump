using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dynatrace.OneAgent.Sdk.Api;
using Microsoft.Extensions.Options;
using SuperDumpService.Helpers;

namespace SuperDumpService.Services.Analyzers {
	public class AnalyzerPipeline {
		public IEnumerable<AnalyzerJob> Analyzers { get; private set; }
		public IEnumerable<InitalAnalyzerJob> InitialAnalyzers { get; private set; }

		public AnalyzerPipeline(
					DumpRepository dumpRepo,
					BundleRepository bundleRepo,
					PathHelper pathHelper,
					IOptions<SuperDumpSettings> settings,
					ElasticSearchService elasticSearch,
					SimilarityService similarityService,
					IOneAgentSdk dynatraceSdk) {

			var analyzers = new List<AnalyzerJob>();
			analyzers.Add(new DumpAnalyzerJob(dumpRepo, settings, pathHelper, dynatraceSdk));
			analyzers.Add(new ElasticSearchJob(bundleRepo, dumpRepo, elasticSearch));
			analyzers.Add(new SimilarityAnalyzerJob(similarityService, settings));

			Analyzers = analyzers;
			InitialAnalyzers = analyzers.Where(analyzerJob => analyzerJob is InitalAnalyzerJob).Cast<InitalAnalyzerJob>();
		}
	}
}
