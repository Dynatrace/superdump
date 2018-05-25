using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.Models {
	public sealed class DumpIdentifier : IEquatable<DumpIdentifier> {
		public string BundleId { get; }
		public string DumpId { get; }

		public DumpIdentifier(string bundleId, string dumpId) {
			this.BundleId = bundleId;
			this.DumpId = dumpId;
		}

		public bool Equals(DumpIdentifier other) { // for IEquatable<Pair>
			return Equals(BundleId, other.BundleId) && Equals(DumpId, other.DumpId);
		}

		public override bool Equals(object other) {
			return (other as DumpIdentifier)?.Equals(this) == true;
		}

		public override int GetHashCode() {
			return (BundleId?.GetHashCode() * 17 + DumpId?.GetHashCode()).GetValueOrDefault();
		}

		public void Deconstruct(out string bundleId, out string dumpId) {
			bundleId = this.BundleId;
			dumpId = this.DumpId;
		}
	}
}
