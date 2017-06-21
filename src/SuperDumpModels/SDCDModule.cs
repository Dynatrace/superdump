using SuperDump.Models;
using System;

namespace SuperDump.Models {
	public class SDCDModule : SDModule, IEquatable<SDCDModule> {
		public ulong Offset { get; set; } = 0;
		public ulong StartAddress { get; set; } = 0;
		public ulong EndAddress { get; set; } = 0;
		public string LocalPath { get; set; } = null;
		public string DebugSymbolPath { get; set; } = null;

		public override bool Equals(object obj) {
			if (obj is SDCDModule) {
				var module = obj as SDCDModule;
				return this.Equals(module);
			}
			return false;
		}

		public bool Equals(SDCDModule other) {
			return base.Equals(other)
				&& this.Offset.Equals(other.Offset)
				&& this.StartAddress.Equals(other.StartAddress)
				&& this.EndAddress.Equals(other.EndAddress);
		}

		public override int GetHashCode() {
			int hash = 17;
			hash = hash * 23 + base.GetHashCode();
			hash = hash * 23 + Offset.GetHashCode();
			hash = hash * 23 + StartAddress.GetHashCode();
			hash = hash * 23 + EndAddress.GetHashCode();
			return hash;
		}
	}
}
