using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperDump.Models {
	[Serializable]
	public class SDMemoryObject : ISerializableJson {
		public string Type { get; set; } = "";
		public ulong Count { get; set; }
		public ulong Size { get; set; }

		public SDMemoryObject() {

		}
		public SDMemoryObject(string type, ulong count, ulong size) {
			this.Type = type;
			this.Count = count;
			this.Size = size;
		}

		public string SerializeToJSON() {
			throw new NotImplementedException();
		}
	}
}
