using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

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

		public override string ToString() {
			return $"{BundleId}:{DumpId}";
		}

		public static bool operator==(DumpIdentifier obj1, DumpIdentifier obj2) {
			if (ReferenceEquals(obj1, obj2)) return true;
			if (ReferenceEquals(obj1, null)) return false;
			if (ReferenceEquals(obj2, null)) return false;
			return (obj1.Equals(obj2));
		}
		
		public static bool operator!=(DumpIdentifier obj1, DumpIdentifier obj2) {
			return !(obj1 == obj2);
		}
	}
}
