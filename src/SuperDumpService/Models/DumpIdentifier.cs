using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SuperDumpService.Models {
	public sealed class DumpIdentifier : IEquatable<DumpIdentifier> {
		private static DumpIdentifierPool pool = new DumpIdentifierPool();

		public string BundleId { get; }
		public string DumpId { get; }

		private DumpIdentifier(string bundleId, string dumpId) {
			this.BundleId = bundleId;
			this.DumpId = dumpId;
		}

		public static DumpIdentifier Create(string bundleId, string dumpId) => pool.Allocate(bundleId, dumpId);

		public static DumpIdentifier Parse(string id) {
			if (id == null) return null;
			if (!id.Contains(":")) return null;
			var parts = id.Split(":");
			if (parts.Length != 2) return null;
			return Create(parts[0], parts[1]);
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

		/// <summary>
		/// Ever-growing pool of DumpIdentifier objects
		/// 
		/// Goal is to allocate only one object per DumpIdentifier and re-use it throughout to save memory
		/// ever-growing, because as of right now, every dump exists in memory
		/// </summary>
		private sealed class DumpIdentifierPool {
			private Dictionary<string, DumpIdentifier> pool = new Dictionary<string, DumpIdentifier>();
			private object sync = new object();

			public DumpIdentifier Allocate(string bundleId, string dumpId) {
				string identifier = $"{bundleId}:{dumpId}";
				lock (sync) {
					if (pool.TryGetValue(identifier, out DumpIdentifier id2)) return id2;
					var newId = new DumpIdentifier(bundleId, dumpId);
					pool.TryAdd(identifier, newId);
					return newId;
				}
			}
		}
	}

	/// <summary>
	/// custom serializer to write in format "bundleId:dumpId"
	/// uses object pool
	/// </summary>
	public class DumpIdentifierConverter : JsonConverter {
		public override bool CanConvert(Type objectType) {
			return (objectType == typeof(DumpIdentifier));
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			string s = (string)reader.Value;
			if (s == null) return null;
			var parts = s.Split(":");
			if (parts.Length != 2) return null;
			return DumpIdentifier.Create(parts[0], parts[1]);
		}

		public override bool CanWrite {
			get { return true; }
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			writer.WriteValue(value.ToString());
		}
	}
}
