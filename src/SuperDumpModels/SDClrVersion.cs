using Newtonsoft.Json;
using System;

namespace SuperDump.Models {
	public class SDClrVersion : IEquatable<SDClrVersion>, ISerializableJson {
		public string Version { get; set; } = "";
		public SDModule DacFile { get; set; }
		public string ClrFlavor { get; set; } = "";

		public SDClrVersion() { }

		public override int GetHashCode() {
			return base.GetHashCode();
		}

		public override bool Equals(object obj) {
			if (obj is SDClrVersion) {
				var version = obj as SDClrVersion;
				return this.Equals(version);
			}
			return false;
		}

		public bool Equals(SDClrVersion other) {
			bool equals = false;

			if (this.ClrFlavor.Equals(other.ClrFlavor)
				&& this.DacFile.Equals(other.DacFile)
				&& this.Version.Equals(other.Version)) {
				equals = true;
			}
			return equals;
		}

		public string SerializeToJSON() {
			return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings {
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			});
		}
	}
}
