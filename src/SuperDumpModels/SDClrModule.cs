using Microsoft.Diagnostics.Runtime;
using Newtonsoft.Json;
using System;

namespace SuperDump.Models {
	[Serializable]
	public class SDClrModule : IEquatable<SDClrModule>, ISerializableJson {
		public ulong AssemblyId { get; set; }
		public string AssemblyName { get; set; }
		public bool IsDynamic { get; set; }
		public bool IsFile { get; set; }
		public ulong Size { get; set; }
		public SDPdbInfo Pdb { get; set; }
		public string Name { get; set; }

		public SDClrModule() {
			this.Pdb = new SDPdbInfo();
		}

		public SDClrModule(ClrModule module) {
			this.AssemblyId = module.AssemblyId;
			this.AssemblyName = module.AssemblyName;
			this.IsDynamic = module.IsDynamic;
			this.IsFile = module.IsFile;
			this.Name = module.Name;
			this.Pdb = new SDPdbInfo();
			if (module.Pdb != null) {
				this.Pdb.FileName = module.Pdb.FileName;
				this.Pdb.Guid = module.Pdb.Guid.ToString();
				this.Pdb.Revision = module.Pdb.Revision;
			}
		}

		public override int GetHashCode() {
			return base.GetHashCode();
		}

		public override bool Equals(object obj) {
			if (obj is SDClrModule) {
				var module = obj as SDClrModule;
				return this.Equals(module);
			}
			return false;
		}

		public bool Equals(SDClrModule other) {
			bool equals = false;

			// if dynamic module, only compare IDs
			if (this.IsDynamic || other.IsDynamic) {
				equals = this.AssemblyId.Equals(other.AssemblyId);
			} else {
				if (AssemblyId.Equals(other.AssemblyId)
					 && this.AssemblyName.Equals(other.AssemblyName)
					 && this.IsDynamic.Equals(other.IsDynamic)
					 && this.IsFile.Equals(other.IsFile)
					 && this.Size.Equals(other.Size)
					 && this.Pdb.Equals(other.Pdb)
					 && this.Name.Equals(other.Name)) {
					equals = true;
				}
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
