using Newtonsoft.Json;
using System;

namespace SuperDump.Models {
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
			return (int)(
				17 * Type.GetHashCode() + 
				31 * Description.GetHashCode() + 
				31 * ThreadId);
		}

		public override bool Equals(object obj) {
			if (obj is SDLastEvent lastEvent) {
				return this.Equals(lastEvent);
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
