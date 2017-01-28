using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SuperDump.Models {
	[Serializable]
	public class SDSystemContext : IEquatable<SDSystemContext>, ISerializableJson {
		public string ProcessArchitecture { get; set; } = "";
		public string SystemArchitecture { get; set; } = "";
		public string DumpTime { get; set; } = "";
		public string SystemUpTime { get; set; } = "";
		public string ProcessUpTime { get; set; } = "";
		public string OSVersion { get; set; } = "";
		public uint NumberOfProcessors { get; set; }
		public IList<SDAppDomain> AppDomains { get; set; }
		public SDAppDomain SharedDomain { get; set; }
		public SDAppDomain SystemDomain { get; set; }
		public IList<SDModule> Modules { get; set; }
		public IList<SDClrVersion> ClrVersions { get; set; }

		public SDSystemContext() { }

		public override bool Equals(object obj) {
			if (obj is SDSystemContext) {
				var systemContext = obj as SDSystemContext;
				return this.Equals(systemContext);
			}
			return false;
		}

		public override int GetHashCode() {
			return base.GetHashCode();
		}

		public bool Equals(SDSystemContext other) {
			bool equals = false;
			if (this.AppDomains.SequenceEqual(other.AppDomains)
				&& this.ClrVersions.SequenceEqual(other.ClrVersions)
				&& this.DumpTime.Equals(other.DumpTime)
				&& this.Modules.SequenceEqual(other.Modules)
				&& this.NumberOfProcessors.Equals(other.NumberOfProcessors)
				&& this.OSVersion.Equals(other.OSVersion)
				&& this.ProcessArchitecture.Equals(other.ProcessArchitecture)
				&& this.ProcessUpTime.Equals(other.ProcessUpTime)
				&& this.SystemArchitecture.Equals(other.SystemArchitecture)
				&& this.SystemUpTime.Equals(other.SystemUpTime)) {
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
