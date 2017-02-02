using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SuperDump.Models {
	public class SDAppDomain : IEquatable<SDAppDomain>, ISerializableJson {
		public ulong Address { get; set; }
		public string ApplicationBase { get; set; } = "";
		public int Id { get; set; }
		public string Name { get; set; }
		public IList<SDClrModule> Modules { get; set; }
		public SDClrVersion Runtime { get; set; }

		public SDAppDomain() { }

		public override int GetHashCode() {
			return base.GetHashCode();
		}

		public override bool Equals(object obj) {
			if (obj is SDAppDomain) {
				var domain = obj as SDAppDomain;
				return this.Equals(domain);
			}
			return false;
		}

		public bool Equals(SDAppDomain other) {
			bool equals = false;
			if (this.Address.Equals(other.Address)
				&& this.ApplicationBase.Equals(other.ApplicationBase)
				&& this.Id.Equals(other.Id)
				&& this.Name.Equals(other.Name)
				&& this.Modules.SequenceEqual(other.Modules)
				&& this.Runtime.Equals(other.Runtime)) {
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
