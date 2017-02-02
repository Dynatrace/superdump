using Newtonsoft.Json;
using System;

namespace SuperDump.Models {
	public class SDPdbInfo : IEquatable<SDPdbInfo>, ISerializableJson {
		public string FileName { get; set; } = "";
		public int Revision { get; set; }
		public string Guid { get; set; } = "";

		public SDPdbInfo() { }

		public SDPdbInfo(string fileName, string guid, int rev) {
			this.FileName = fileName;
			this.Guid = guid;
			this.Revision = rev;
		}

		public override int GetHashCode() {
			return base.GetHashCode();
		}

		public override bool Equals(object obj) {
			if (obj is SDPdbInfo) {
				var info = obj as SDPdbInfo;
				return this.Equals(info);
			}
			return false;
		}

		public bool Equals(SDPdbInfo other) {
			return this.FileName.Equals(other.FileName) && this.Revision.Equals(other.Revision) && this.Guid.Equals(other.Guid);
		}

		public string SerializeToJSON() {
			return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings {
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			});
		}
	}
}
