using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace SuperDump.Models {
	public class SDModule : IEquatable<SDModule>, ISerializableJson, ITaggableItem {
		public string Version { get; set; }
		public ulong ImageBase { get; set; }
		public string FilePath { get; set; }
		private string fileName;
		public string FileName {
			get {
				if (fileName == null)
					return Path.GetFileName(this.FilePath);
				else
					return this.fileName;
			}
			set { fileName = value; }
		}

		public uint FileSize { get; set; }
		public bool IsManaged { get; set; }
		public uint TimeStamp { get; set; }

		public SDPdbInfo PdbInfo { get; set; }
		public ISet<SDTag> Tags { get; } = new HashSet<SDTag>();

		public SDModule() {
			this.PdbInfo = new SDPdbInfo();
		}

		public override int GetHashCode() {
            int hash = 17;
            hash = hash * 23 + Version.GetHashCode();
            hash = hash * 23 + ImageBase.GetHashCode();
            hash = hash * 23 + FilePath.GetHashCode();
            hash = hash * 23 + FileName.GetHashCode();
            hash = hash * 23 + FileSize.GetHashCode();
            hash = hash * 23 + IsManaged.GetHashCode();
            hash = hash * 23 + TimeStamp.GetHashCode();
            hash = hash * 23 + PdbInfo.GetHashCode();
            hash = hash * 23 + Tags.GetHashCode();
            return hash;
        }

		public override bool Equals(object obj) {
			if (obj is SDModule) {
				var module = obj as SDModule;
				return this.Equals(module);
			}
			return false;
		}

		public bool Equals(SDModule other) {
			bool equals = false;
			if (this.FileName.Equals(other.FileName)
				&& this.FileSize.Equals(other.FileSize)
				&& this.ImageBase.Equals(other.ImageBase)
				&& this.IsManaged.Equals(other.IsManaged)
				&& this.PdbInfo.Equals(other.PdbInfo)
				&& this.TimeStamp.Equals(other.TimeStamp)
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
