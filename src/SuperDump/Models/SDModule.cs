using Microsoft.Diagnostics.Runtime;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperDump.Models {
	[Serializable]
	public class SDModule : IEquatable<SDModule>, ISerializableJson {

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
			set {
				fileName = value;
			}
		}
		public uint FileSize { get; set; }
		public bool IsManaged { get; set; }
		public uint TimeStamp { get; set; }

		public SDPdbInfo PdbInfo { get; set; }

		public SDModule() {
			this.PdbInfo = new SDPdbInfo();
		}
		public SDModule(ModuleInfo info) {
			this.FilePath = info.FileName;
			this.FileSize = info.FileSize;
			this.ImageBase = info.ImageBase;
			this.IsManaged = info.IsManaged;
			this.TimeStamp = info.TimeStamp;
			this.Version = info.Version.ToString();
			this.PdbInfo = new SDPdbInfo();
			if (info.Pdb != null) {
				this.PdbInfo.FileName = info.Pdb.FileName;
				this.PdbInfo.Guid = info.Pdb.Guid.ToString();
				this.PdbInfo.Revision = info.Pdb.Revision;
			}
		}
		public override int GetHashCode() {
			return base.GetHashCode();
		}
		public override bool Equals(object obj) {
			if(obj is SDModule) {
				var module = obj as SDModule;
				return this.Equals(module);
			}
			return false;
		}
		public bool Equals(SDModule other) {
			bool equals = false;
			if(this.FileName.Equals(other.FileName)
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
