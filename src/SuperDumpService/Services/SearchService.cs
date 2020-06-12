using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SuperDumpService.Models;
using SuperDumpService.ViewModels;

namespace SuperDumpService.Services {
	public class SearchService {
		private readonly BundleRepository bundleRepo;
		private readonly DumpRepository dumpRepo;
		private readonly SimilarityService similarityService;
		private readonly ElasticSearchService elasticService;
		private readonly JiraIssueRepository jiraIssueRepository;
		private readonly SuperDumpSettings settings;

		public SearchService(
				BundleRepository bundleRepo,
				DumpRepository dumpRepo,
				SimilarityService similarityService,
				ElasticSearchService elasticService,
				IOptions<SuperDumpSettings> settings,
				JiraIssueRepository jiraIssueRepository) {
			this.bundleRepo = bundleRepo;
			this.dumpRepo = dumpRepo;
			this.similarityService = similarityService;
			this.elasticService = elasticService;
			this.jiraIssueRepository = jiraIssueRepository;
			this.settings = settings.Value;
		}

		public async Task<IOrderedEnumerable<DumpViewModel>> SearchBySimpleFilter(string searchFilter, bool includeSimilarities = true) {
			var dumps = await Task.WhenAll(dumpRepo.GetAll().Select(x => ToDumpViewModel(x, includeSimilarities)));
			var filtered = SimpleFilter(searchFilter, dumps).OrderByDescending(x => x.DumpInfo.Created);
			return filtered;
		}

		public async Task<IOrderedEnumerable<DumpViewModel>> SearchByElasticFilter(string elasticSearchFilter, bool includeSimilarities = true) {
			var searchResults = elasticService.SearchDumpsByJson(elasticSearchFilter).ToList();
			IEnumerable<DumpViewModel> dumpViewModels = await Task.WhenAll(searchResults.Select(x => ToDumpViewModel(x, includeSimilarities)));
			dumpViewModels = dumpViewModels.Where(x => x != null); // if elasticsearch contains entries that arent found in repo, just filter those null entries
			var dumpViewModelsOrdered = dumpViewModels.OrderByDescending(x => x.DumpInfo.Created);
			return dumpViewModelsOrdered;
		}

		public async Task<IOrderedEnumerable<DumpViewModel>> SearchDuplicates(DumpIdentifier id, bool includeSimilarities = true) {
			var similarDumps = new Similarities(await similarityService.GetSimilarities(id)).AboveThresholdSimilarities().Select(x => x.Key);
			var dumpViewModels = await Task.WhenAll(similarDumps.Select(x => ToDumpViewModel(x, includeSimilarities)));
			var dumpViewModelsOrdered = dumpViewModels.Where(x => x != null).OrderByDescending(x => x.DumpInfo.Created);
			return dumpViewModelsOrdered;
		}

		private async Task<DumpViewModel> ToDumpViewModel(ElasticSDResult elasticSDResult, bool includeSimilarities = false) {
			return await ToDumpViewModel(elasticSDResult.DumpIdentifier, includeSimilarities);
		}

		private async Task<DumpViewModel> ToDumpViewModel(DumpIdentifier id, bool includeSimilarities = false) {
			return await ToDumpViewModel(dumpRepo.Get(id), includeSimilarities);
		}

		private async Task<DumpViewModel> ToDumpViewModel(DumpMetainfo dumpMetainfo, bool includeSimilarities = false) {
			if (dumpMetainfo == null) return null;
			var similarities = !includeSimilarities ? null : new Similarities(await similarityService.GetSimilarities(dumpMetainfo.Id));
			return new DumpViewModel(dumpMetainfo,
				new BundleViewModel(bundleRepo.Get(dumpMetainfo.BundleId)),
				similarities,
				new RetentionViewModel(
					dumpMetainfo,
					dumpRepo.IsPrimaryDumpAvailable(dumpMetainfo.Id),
					TimeSpan.FromDays(settings.WarnBeforeDeletionInDays),
					settings.UseJiraIntegration && jiraIssueRepository.IsPopulated && jiraIssueRepository.HasBundleOpenIssues(dumpMetainfo.BundleId)));
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
