using Microsoft.Extensions.Options;
using Nest;
using SuperDump.Models;
using SuperDumpService.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDumpService.Services {
	public class ElasticSearchService {
		private const string RESULT_IDX = "sdresults";

		private readonly ElasticClient elasticClient;

		private readonly DumpRepository dumpRepo;
		private readonly BundleRepository bundleRepo;

		public ElasticSearchService(DumpRepository dumpRepo, BundleRepository bundleRepo, IOptions<SuperDumpSettings> settings) {
			this.dumpRepo = dumpRepo;
			this.bundleRepo = bundleRepo;

			string host = settings.Value.ElasticSearchHost;
			if(host == null || host == "") {
				elasticClient = null;
				return;
			}
			ConnectionSettings connSettings = new ConnectionSettings(new Uri(settings.Value.ElasticSearchHost))
				.MapDefaultTypeIndices(m => m.Add(typeof(ElasticSDResult), RESULT_IDX));
			elasticClient = new ElasticClient(connSettings);

			if (!elasticClient.IndexExists(RESULT_IDX).Exists) {
				elasticClient.CreateIndex(RESULT_IDX, i => i.Mappings(m => m.Map<ElasticSDResult>(ms => ms.AutoMap())));
			}
		}

		[Hangfire.Queue("elasticsearch", Order = 3)]
		public async Task PushAllResultsAsync() {
			if(elasticClient == null) {
				throw new InvalidOperationException("ElasticSearch has not been initialized! Please verify that the settings specify a correct elastic search host.");
			}

			int nErrorsLogged = 0;
			var bundles = bundleRepo.GetAll();
			// Note that this ES push can be improved significantly by using bulk operations to create the documents
			foreach (BundleMetainfo bundle in bundles) {
				var dumps = dumpRepo.Get(bundle.BundleId);
				if(dumps == null) {
					throw new InvalidOperationException("Dump repository must be populated before pushing data into ES.");
				}
				foreach (DumpMetainfo dump in dumps) {
					SDResult result = dumpRepo.GetResult(bundle.BundleId, dump.DumpId, out string error);
					if (result != null) {
						bool success = await PushResultAsync(result, bundle, dump);
						if(!success && nErrorsLogged < 20) {
							Console.WriteLine($"Failed to create document for {dump.BundleId}/{dump.DumpId}");
							nErrorsLogged++;
						}
					}
				}
			}
		}

		public async Task<bool> PushResultAsync(SDResult result, BundleMetainfo bundleInfo, DumpMetainfo dumpInfo) {
			if (elasticClient != null) {
				var response = await elasticClient.CreateAsync(ElasticSDResult.FromResult(result, bundleInfo, dumpInfo));
				if (!response.Created) {
					return false;
				}
			}
			return true;
		}
	}
}
