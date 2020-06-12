using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SuperDump.Models;
using SuperDumpService.Models;

namespace SuperDumpService.Services.Clustering {


	public class DumpClusterCommonPropertiesBuilder {
		private readonly ISet<DumpIdentifier> dumpIds;

		public DumpClusterCommonPropertiesBuilder(ISet<DumpIdentifier> dumpIds) {
			this.dumpIds = dumpIds;
		}

		public async Task<DumpClusterCommonProperties> Create(DumpRepository dumpRepository) {
			var results = new List<Tuple<DumpMetainfo, SDResult>>();
			foreach (var id in dumpIds) { // TODO CN: this could be a performance problem. if it gets too bad, maybe sampling might help. so, if a cluster is too big, only take 100 random samples and analyze them, instead of all
				var result = await dumpRepository.GetResult(id);
				var metainfo = dumpRepository.Get(id);
				if (result != null & metainfo != null) results.Add(Tuple.Create(metainfo, result));
			}

			// all results are populated.
			var statsHostBuilder = new StatsHostBuilder(dumpIds.Count);

			statsHostBuilder.AddStat("DumpFileName", results.Select(x => Path.GetFileName(x.Item1.DumpFileName))); // dump filename often equals the name of the crashing process (not always!!!)
			statsHostBuilder.AddStat("SystemArchitecture", results.Select(x => x.Item2.SystemContext.SystemArchitecture));
			statsHostBuilder.AddStat("ProcArchitecture", results.Select(x => x.Item2.SystemContext.ProcessArchitecture));
			statsHostBuilder.AddStat("Executable", results.Select(x => (x.Item2.SystemContext as SDCDSystemContext)?.FileName)); // linux only
			statsHostBuilder.AddStat("ProcessArgs", results.Select(x => (x.Item2.SystemContext as SDCDSystemContext)?.FileName)); // linux only

			return statsHostBuilder.Create();
		}
	}

	public class StatsHostBuilder {
		private IDictionary<string, StatsBuilder> stats = new Dictionary<string, StatsBuilder>();
		private int totalCount;

		public StatsHostBuilder(int totalCount) {
			this.totalCount = totalCount;
		}

		public void AddStat(string name, IEnumerable<string> instances) {
			stats[name] = new StatsBuilder(name, totalCount, instances);
		}

		internal DumpClusterCommonProperties Create() {
			return new DumpClusterCommonProperties(stats.Select(x => new DumpClusterCommonProperty(x.Key, x.Value.GetTopX(3), x.Value.DistinctValueCount)));
		}
	}

	public class StatsBuilder {
		private IDictionary<string, int> instanceCounts = new Dictionary<string, int>();
		private int totalCount;

		public StatsBuilder(string name, int totalCount, IEnumerable<string> instances) {
			this.totalCount = totalCount;
			foreach (var i in instances) {
				if (i == null) continue;
				if (!instanceCounts.ContainsKey(i)) instanceCounts.Add(i, 0);
				instanceCounts[i]++;
			}
		}

		/// <summary>
		/// Returns top X elements, along with the percentage number of their occurrence.
		/// E.g. if there is only one unique instance, this will return only one tuple, with 1.0 as value.
		/// </summary>
		public IEnumerable<PropertyEntry> GetTopX(int count) {
			return instanceCounts.OrderByDescending(x => x.Value).Take(count).Select(x => new PropertyEntry(x.Key, x.Value / (double)totalCount));
		}

		public int DistinctValueCount => instanceCounts.Count;
	}

	/// <summary>
	/// Datastructure to help maintain and build up a set of clusters
	/// </summary>
	public class DumpClusterHeapBuilder {
		private Dictionary<DumpClusterBuilder, DumpClusterBuilder> clusters = new Dictionary<DumpClusterBuilder, DumpClusterBuilder>();
		private Dictionary<DumpIdentifier, DumpClusterBuilder> dumpIdToCluster = new Dictionary<DumpIdentifier, DumpClusterBuilder>();

		public DumpClusterBuilder GetClusterByDumpId(DumpIdentifier id) => dumpIdToCluster[id];

		public void AddDumpToCluster(DumpClusterBuilder dumpCluster, DumpIdentifier id) {
			dumpCluster.AddDump(id);
			dumpIdToCluster[id] = dumpCluster;
		}

		public DumpClusterBuilder GetOrCreateClusterByDumpId(DumpIdentifier id) {
			DumpClusterBuilder cluster;
			if (!dumpIdToCluster.TryGetValue(id, out cluster)) {
				cluster = new DumpClusterBuilder();
				clusters.Add(cluster, cluster);
				AddDumpToCluster(cluster, id);
			}
			return cluster;
		}

		public void AddAllDumpsToCluster(DumpClusterBuilder dumpCluster, IDictionary<DumpIdentifier, double> relationships, double threshold) {
			foreach (var relation in relationships) {
				if (relation.Value > threshold) AddDumpToCluster(dumpCluster, relation.Key);
			}
		}

		internal async Task<DumpClusterHeap> ToClusterHeap(DumpRepository dumpRepository) {
			return new DumpClusterHeap(await Task.WhenAll(clusters.Select(cb => cb.Value.CreateDumpCluster(dumpRepository))));
		}
	}

	/// <summary>
	/// Contains all the logic to calculate clusters
	/// </summary>
	public class DumpClusterCalculator {

		public async Task<DumpClusterHeapBuilder> CalculateClusters(IEnumerable<DumpMetainfo> dumpInfos, RelationshipRepository relationshipRepository) {
			// Algorithm:
			//    * loop through all dumps, start with most recent.
			//    * from all >X% similar dumps, create a cluster
			//    * move on to the next one. if it's already part of a cluster, keep it there. still, loop through all >X% similar dumps and add it.

			var clusterHeap = new DumpClusterHeapBuilder();

			foreach (var dumpInfo in dumpInfos.OrderByDescending(x => x.Created)) {
				var relationships = await relationshipRepository.GetRelationShips(dumpInfo.Id);
				var cluster = clusterHeap.GetOrCreateClusterByDumpId(dumpInfo.Id);
				clusterHeap.AddAllDumpsToCluster(cluster, relationships, 0.80);
			}

			return clusterHeap;
		}
	}

	/// <summary>
	/// Holds a list of dumps that belong to that cluster. Simple Domain Object.
	/// </summary>
	public class DumpClusterBuilder {
		private ISet<DumpIdentifier> dumpIds = new HashSet<DumpIdentifier>();

		public bool ContainsDump(DumpIdentifier id) => dumpIds.Contains(id);
		public void AddDump(DumpIdentifier id) => dumpIds.Add(id);

		internal async Task<DumpCluster> CreateDumpCluster(DumpRepository dumpRepository) {
			var dumpInfos = dumpIds.Select(id => dumpRepository.Get(id));
			return new DumpCluster(
				dumpIds,
				dumpInfos.Where(x => x != null).Max(x => x.Created),
				dumpInfos.Where(x => x != null).Min(x => x.Created),
				await new DumpClusterCommonPropertiesBuilder(dumpIds).Create(dumpRepository),
				dumpInfos.Where(x => x != null).OrderByDescending(x => x.Created).Take(10).ToList()
			);
		}
	}
}
