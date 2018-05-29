using Microsoft.Extensions.Options;
using Nest;
using SuperDump.Models;
using SuperDumpService.Helpers;
using SuperDumpService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDumpService.Services {
	public class ElasticSearchService {
		private const string RESULT_IDX = "sdresults";

		private readonly ElasticClient elasticClient;

		private readonly DumpRepository dumpRepo;
		private readonly BundleRepository bundleRepo;
		private readonly PathHelper pathHelper;

		public ElasticSearchService(DumpRepository dumpRepo, BundleRepository bundleRepo, PathHelper pathHelper, IOptions<SuperDumpSettings> settings) {
			this.dumpRepo = dumpRepo ?? throw new NullReferenceException("DumpRepository must not be null!");
			this.bundleRepo = bundleRepo ?? throw new NullReferenceException("BundleRepository must not be null!");
			this.pathHelper = pathHelper ?? throw new NullReferenceException("PathHelper must not be null!");

			string host = settings.Value.ElasticSearchHost;
			if (string.IsNullOrEmpty(host)) {
				elasticClient = null;
				return;
			}
			ConnectionSettings connSettings = new ConnectionSettings(new Uri(settings.Value.ElasticSearchHost));
			((IConnectionSettingsValues)connSettings).DefaultIndices.Add(typeof(ElasticSDResult), RESULT_IDX);

			elasticClient = new ElasticClient(connSettings);

			if (!IndexExists()) {
				CreateIndex();
			}
		}

		[Hangfire.Queue("elasticsearch", Order = 3)]
		public async Task PushAllResultsAsync(bool clean) {
			if (elasticClient == null) {
				throw new InvalidOperationException("ElasticSearch has not been initialized! Please verify that the settings specify a correct elastic search host.");
			}

			if (clean) {
				DeleteIndex();
				CreateIndex();
			}
			IEnumerable<string> documentIds = GetAllDocumentIds();

			int nErrorsLogged = 0;
			var bundles = bundleRepo.GetAll();
			if (bundles == null) {
				throw new InvalidOperationException("Bundle repository must be populated before pushing data into ES.");
			}
			// Note that this ES push can be improved significantly by using bulk operations to create the documents
			foreach (BundleMetainfo bundle in bundles) {
				var dumps = dumpRepo.Get(bundle.BundleId);
				if (dumps == null) {
					continue;
				}
				foreach (DumpMetainfo dump in dumps) {
					if (documentIds.Contains(bundle.BundleId + "/" + dump.DumpId)) {
						continue;
					}
					SDResult result = await dumpRepo.GetResult(bundle.BundleId, dump.DumpId);
					if (result != null) {
						bool success = await PushResultAsync(result, bundle, dump);
						if (!success && nErrorsLogged < 20) {
							Console.WriteLine($"Failed to create document for {dump.BundleId}/{dump.DumpId}");
							nErrorsLogged++;
						}
					}
				}
			}
		}

		public async Task<bool> PushResultAsync(SDResult result, BundleMetainfo bundleInfo, DumpMetainfo dumpInfo) {
			if (elasticClient != null) {
				new Nest.CreateRequest<ElasticSDResult>(RESULT_IDX);
				var response = await elasticClient.CreateAsync(new CreateDescriptor<ElasticSDResult>(ElasticSDResult.FromResult(result, bundleInfo, dumpInfo, pathHelper)));
				if (response.Result != Result.Created) {
					return false;
				}
			}
			return true;
		}

		private bool IndexExists() {
			return elasticClient.IndexExists(RESULT_IDX).Exists;
		}

		private void CreateIndex() {
			elasticClient.CreateIndex(RESULT_IDX, i => i.Mappings(m => m.Map<ElasticSDResult>(ms => ms.AutoMap())));
		}

		private void DeleteIndex() {
			elasticClient.DeleteIndex(Indices.Index("sdresults"));
		}

		private IEnumerable<string> GetAllDocumentIds() {
			List<string> ids = new List<string>();
			// Get the documents in 10.000 steps because this is the max number of documents to be retrieved at once from ES
			for (int i = 0; true; i++) {
				var result = elasticClient.Search<ElasticSDResult>(s =>
					s.Source(src => src.Includes(e => e.Field(p => p.Id).Field(p => p.BundleId).Field(p => p.DumpId)))
					.Query(q => q.MatchAll())
					.From(i*10000).Size(10000));
				if (result.Documents.Count == 0) {
					break;
				}
				ids.AddRange(result.Documents.Select(doc => doc.Id));
			}
			return ids;
		}
	}
}
