using Elasticsearch.Net;
using Microsoft.Extensions.Options;
using Nest;
using SuperDump.Models;
using SuperDumpService.Helpers;
using SuperDumpService.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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

		private IEnumerable<ElasticSDResult> GetBatch(IEnumerable<ElasticSDResult> list, int pageNumber) {
			return list.Skip(pageNumber * 1000).Take(1000);
		}

		[Hangfire.Queue("elasticsearch", Order = 3)]
		public async Task PushAllResultsAsync(bool clean) {
			if (elasticClient == null) {
				throw new InvalidOperationException("ElasticSearch has not been initialized! Please verify that the settings specify a correct elastic search host.");
			}

			if (clean) {
				DeleteIndex();
				CreateIndex();

				// since we are clean, we can do everything in one bulk
				var dumps = dumpRepo.GetAll().OrderByDescending(x => x.Created);
				foreach (var dumpsBatch in dumps.Batch(100)) {
					var results = (await Task.WhenAll(dumpsBatch.Select(async x =>
						new { res = await dumpRepo.GetResult(x.Id), bundleInfo = bundleRepo.Get(x.BundleId), dumpInfo = x })))
						.Where(x => x.res != null);
					Console.WriteLine($"pushing {results.Count()} results into elasticsearch");
					await PushBulk(results.Select(x => ElasticSDResult.FromResult(x.res, x.bundleInfo, x.dumpInfo, pathHelper)));
				}
				return;
			}

			IEnumerable<string> documentIds = GetAllDocumentIds();

			int nErrorsLogged = 0;
			var bundles = bundleRepo.GetAll();
			if (bundles == null) {
				throw new InvalidOperationException("Bundle repository must be populated before pushing data into ES.");
			}

			// In order to check if a dump has already been added, we go through them all and add one at the time
			// There is potential to optimize this and still do a bulk add.
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

		public async Task<bool> PushBulk(IEnumerable<ElasticSDResult> results) {
			if (elasticClient == null) return false;
			var descriptor = new BulkDescriptor();
			descriptor.CreateMany<ElasticSDResult>(results);
			Console.WriteLine($"Inserting {results.Count()} superdump results into ES...");
			var sw = new Stopwatch();
			sw.Start();

			var result = await elasticClient.BulkAsync(descriptor);

			sw.Stop();
			Console.WriteLine($"Finished inserting in {sw.Elapsed}");
			return true;
		}

		public async Task<bool> PushResultAsync(SDResult result, BundleMetainfo bundleInfo, DumpMetainfo dumpInfo) {
			try {
				if (elasticClient != null && result != null) {
					new Nest.CreateRequest<ElasticSDResult>(RESULT_IDX);
					var response = await elasticClient.CreateAsync(new CreateDescriptor<ElasticSDResult>(ElasticSDResult.FromResult(result, bundleInfo, dumpInfo, pathHelper)));

					if (response.Result != Result.Created) {
						return false;
					}
				}
				return true;
			} catch (Exception e) {
				Console.WriteLine($"PushResultAsync failed for {dumpInfo.Id} with exception: {e}");
				return false;
			}
		}

		internal IEnumerable<ElasticSDResult> SearchDumpsByJson(string jsonQuery) {
			var result = elasticClient.LowLevel.Search<SearchResponse<dynamic>>(jsonQuery);
			if (!result.IsValid) {
				throw new Exception($"elastic search query failed: {result.DebugInformation}");
			}
			foreach (var res in result.Documents) {
				ElasticSDResult deserialized = null;
				try {
					string str = Convert.ToString(res);
					using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(str))) {
						deserialized = elasticClient.RequestResponseSerializer.Deserialize<ElasticSDResult>(stream);
					}
				} catch (Exception e) {
					Console.WriteLine("error deserializing elasticsearch result");
				}
				if (deserialized != null) yield return deserialized;
			}
			//SearchRequest searchRequest;
			//using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonQuery))) {
			//	searchRequest = elasticClient.RequestResponseSerializer.Deserialize<SearchRequest>(stream);
			//}
			//var result = elasticClient.Search<ElasticSDResult>(searchRequest);
			//return Enumerable.Empty<ElasticSDResult>();
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
					.From(i * 10000).Size(10000));
				if (result.Documents.Count == 0) {
					break;
				}
				ids.AddRange(result.Documents.Select(doc => doc.Id));
			}
			return ids;
		}
	}
}
