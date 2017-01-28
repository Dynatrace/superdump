using Microsoft.Diagnostics.Runtime;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperDump.Models {
	[Serializable]
	public class SDClrVersion : IEquatable<SDClrVersion>, ISerializableJson {
		public string Version { get; set; } = "";
		public SDModule DacFile { get; set; }
		public string ClrFlavor { get; set; } = "";

		public SDClrVersion() {

		}
		public SDClrVersion(ClrInfo info) {
			if (info != null) {
				this.Version = info.Version.ToString();
				this.ClrFlavor = info.Flavor.ToString();

				// save DAC info 
				SDModule dac = new SDModule();
				dac.FileSize = info.DacInfo.FileSize;
				dac.FilePath = info.DacInfo.FileName;
				dac.ImageBase = info.DacInfo.ImageBase;
				dac.IsManaged = info.DacInfo.IsManaged;
				dac.TimeStamp = info.DacInfo.TimeStamp;
				dac.Version = info.DacInfo.Version.ToString();

				// save PDB info, if avaliable
				if (info.DacInfo.Pdb != null) {
					dac.PdbInfo = new SDPdbInfo(info.DacInfo.Pdb.FileName, 
						info.DacInfo.Pdb.Guid.ToString(), 
						info.DacInfo.Pdb.Revision);
				}

				this.DacFile = dac;
			}
		}
		public override int GetHashCode() {
			return base.GetHashCode();
		}
		public override bool Equals(object obj) {
			if(obj is SDClrVersion) {
				var version = obj as SDClrVersion;
				return this.Equals(version);
			}
			return false;
		}
		public bool Equals(SDClrVersion other) {
			bool equals = false;

			if(this.ClrFlavor.Equals(other.ClrFlavor)
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
