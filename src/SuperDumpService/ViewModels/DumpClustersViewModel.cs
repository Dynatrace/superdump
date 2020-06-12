using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SuperDumpService.Services;
using SuperDumpService.Services.Clustering;
using X.PagedList;

namespace SuperDumpService.ViewModels {
	public class DumpClusterHeapViewModel {
		public IPagedList<DumpClusterViewModel> Paged { get; set; }

		public DumpClusterHeapViewModel(DumpClusterHeap dumpClusterHeap) {
			Paged = dumpClusterHeap.Clusters.Select(x => new DumpClusterViewModel(x)).OrderBy(x => x.Cluster.Latest).ToPagedList();
		}
	}

	public class DumpClusterViewModel {
		public DumpCluster Cluster { get; }

		public DumpClusterViewModel(DumpCluster cluster) {
			this.Cluster = cluster;
		}
	}
}
