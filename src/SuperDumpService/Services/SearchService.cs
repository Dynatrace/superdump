using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SuperDumpService.Models;
using SuperDumpService.ViewModels;

namespace SuperDumpService.Services {
	public class SearchService {
		private readonly BundleRepository bundleRepo;
		private readonly DumpRepository dumpRepo;
		private readonly SimilarityService similarityService;
		private readonly ElasticSearchService elasticService;

		public SearchService(
				BundleRepository bundleRepo,
				DumpRepository dumpRepo,
				SimilarityService similarityService,
				ElasticSearchService elasticService) {
			this.bundleRepo = bundleRepo;
			this.dumpRepo = dumpRepo;
			this.similarityService = similarityService;
			this.elasticService = elasticService;
		}

		public async Task<IOrderedEnumerable<DumpViewModel>> SearchBySimpleFilter(string searchFilter, bool includeSimilarities = true) {
			var dumps = await Task.WhenAll(dumpRepo.GetAll().Select(x => ToDumpViewModel(x, dumpRepo, bundleRepo, includeSimilarities ? similarityService : null)));
			var filtered = SimpleFilter(searchFilter, dumps).OrderByDescending(x => x.DumpInfo.Created);
			return filtered;
		}

		public async Task<IOrderedEnumerable<DumpViewModel>> SearchByElasticFilter(string elasticSearchFilter, bool includeSimilarities = true) {
			var searchResults = elasticService.SearchDumpsByJson(elasticSearchFilter).ToList();
			IEnumerable<DumpViewModel> dumpViewModels = await Task.WhenAll(searchResults.Select(x => ToDumpViewModel(x, dumpRepo, bundleRepo, includeSimilarities ? similarityService : null)));
			dumpViewModels = dumpViewModels.Where(x => x != null); // if elasticsearch contains entries that arent found in repo, just filter those null entries
			var dumpViewModelsOrdered = dumpViewModels.OrderByDescending(x => x.DumpInfo.Created);
			return dumpViewModelsOrdered;
		}

		public async Task<IOrderedEnumerable<DumpViewModel>> SearchDuplicates(DumpIdentifier id, bool includeSimilarities = true) {
			var similarDumps = new Similarities(await similarityService.GetSimilarities(id)).AboveThresholdSimilarities().Select(x => x.Key);
			var dumpViewModels = await Task.WhenAll(similarDumps.Select(x => ToDumpViewModel(x, dumpRepo, bundleRepo, includeSimilarities ? similarityService : null)));
			var dumpViewModelsOrdered = dumpViewModels.OrderByDescending(x => x.DumpInfo.Created);
			return dumpViewModelsOrdered;
		}

		private static async Task<DumpViewModel> ToDumpViewModel(ElasticSDResult elasticSDResult, DumpRepository dumpRepo, BundleRepository bundleRepo, SimilarityService similarityService = null) {
			return await ToDumpViewModel(elasticSDResult.DumpIdentifier, dumpRepo, bundleRepo, similarityService);
		}

		private static async Task<DumpViewModel> ToDumpViewModel(DumpIdentifier id, DumpRepository dumpRepo, BundleRepository bundleRepo, SimilarityService similarityService = null) {
			return await ToDumpViewModel(dumpRepo.Get(id), dumpRepo, bundleRepo, similarityService);
		}

		private static async Task<DumpViewModel> ToDumpViewModel(DumpMetainfo dumpMetainfo, DumpRepository dumpRepo, BundleRepository bundleRepo, SimilarityService similarityService = null) {
			if (dumpMetainfo == null) return null;
			var similarities = similarityService == null ? null : new Similarities(await similarityService.GetSimilarities(dumpMetainfo.Id));
			return new DumpViewModel(dumpMetainfo, new BundleViewModel(bundleRepo.Get(dumpMetainfo.BundleId)), similarities);
		}

		public static IEnumerable<DumpViewModel> SimpleFilter(string searchFilter, IEnumerable<DumpViewModel> dumps) {
			if (searchFilter == null) return dumps;
			return dumps.Where(d =>
				   d.DumpInfo.DumpId.Contains(searchFilter, StringComparison.OrdinalIgnoreCase)
				|| d.DumpInfo.BundleId.Contains(searchFilter, StringComparison.OrdinalIgnoreCase)
				|| d.DumpInfo.DumpFileName != null && d.DumpInfo.DumpFileName.Contains(searchFilter, StringComparison.OrdinalIgnoreCase)
				|| d.BundleViewModel.CustomProperties.Any(cp => cp.Value != null && cp.Value.Contains(searchFilter, StringComparison.OrdinalIgnoreCase))
			);
		}
	}
}
