using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SuperDumpService.Models;

namespace SuperDumpService.Services.Clustering {

	/// <summary>
	/// immutable domain object
	/// </summary>
	public class DumpClusterHeap {
		public DumpCluster[] Clusters { get; }

		public DumpClusterHeap(IEnumerable<DumpCluster> clusters) {
			this.Clusters = clusters.ToArray();
		}
	}

	/// <summary>
	/// immutable domain object
	/// </summary>
	public class DumpCluster {
		public ISet<DumpIdentifier> DumpIds { get; }
		public DumpClusterCommonProperties CommonProperties { get; }
		public DateTime First { get; }
		public DateTime Latest { get; }
		public IList<DumpMetainfo> LastX { get; }

		public DumpCluster(ISet<DumpIdentifier> dumpIds, DateTime first, DateTime latest, DumpClusterCommonProperties commonProperties, IList<DumpMetainfo> lastX) {
			DumpIds = dumpIds;
			First = first;
			Latest = latest;
			CommonProperties = commonProperties;
			LastX = lastX;
		}

		public override string ToString() {
			return $"{DumpIds.Count} dumps, First: {First}, Latest: {Latest}";
		}
	}

	/// <summary>
	/// Holds a set of properties that are common to some degree in one cluster. Simple Domain Object.
	/// E.g. 100% of all dumps in this cluster are on OS=Windows, Bitness=32-bit
	/// </summary>
	public class DumpClusterCommonProperties {
		public IEnumerable<DumpClusterCommonProperty> Properties { get; }

		public DumpClusterCommonProperties(IEnumerable<DumpClusterCommonProperty> properties) {
			this.Properties = properties;
		}
	}

	public class DumpClusterCommonProperty {
		public string Name { get; }
		public IEnumerable<PropertyEntry> TopX { get; }
		public int DistinctCount { get; }

		public DumpClusterCommonProperty(string name, IEnumerable<PropertyEntry> topX, int distinctCount) {
			Name = name;
			TopX = topX;
			DistinctCount = distinctCount;
		}
	}

	public class PropertyEntry {
		public string Value { get; }
		public double Percentage { get; }
		public PropertyEntry(string value, double percenatge) {
			this.Value = value;
			this.Percentage = percenatge;
		}
	}
}
