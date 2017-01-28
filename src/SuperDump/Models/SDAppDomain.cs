using Microsoft.Diagnostics.Runtime;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperDump.Models {
	[Serializable]
	public class SDAppDomain : IEquatable<SDAppDomain>, ISerializableJson {
		public ulong Address { get; set; }
		public string ApplicationBase { get; set; } = "";
		public int Id { get; set; }
		public string Name { get; set; }
		public IList<SDClrModule> Modules { get; set; }
		public SDClrVersion Runtime { get; set; }

		public SDAppDomain() {

		}
		public SDAppDomain(ClrAppDomain domain) {
			this.Address = domain.Address;
			this.ApplicationBase = domain.ApplicationBase;
			this.Id = domain.Id;
			this.Name = domain.Name;
			this.Modules = new List<SDClrModule>();
			foreach (ClrModule clrModule in domain.Modules) {
				this.Modules.Add(new SDClrModule(clrModule));
			}
			this.Runtime = new SDClrVersion(domain.Runtime.ClrInfo);
		}
		public override int GetHashCode() {
			return base.GetHashCode();
		}
		public override bool Equals(object obj) {
			if(obj is SDAppDomain) {
				var domain = obj as SDAppDomain;
				return this.Equals(domain);
			}
			return false;
		}
		public bool Equals(SDAppDomain other) {
			bool equals = false;
			if(this.Address.Equals(other.Address)
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
