using Newtonsoft.Json;
using System;

namespace SuperDump.Models {
	[Serializable]
	public class SDLastEvent : IEquatable<SDLastEvent>, ISerializableJson {
		public string Type { get; set; } = "";
		public string Description { get; set; } = "";
		public uint ThreadId { get; set; }

		public SDLastEvent() { }

		public SDLastEvent(string type, string description, uint threadId) {
			this.Type = type;
			this.Description = description;
			this.ThreadId = threadId;
		}

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

		public bool Equals(SDLastEvent other) {
			bool equals = false;

			if (this.Type.Equals(other.Type)
				&& this.Description.Equals(other.Description)
				&& this.ThreadId.Equals(other.ThreadId)) {
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
