using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperDump.Models {
	// SDResult identifies the whole result of the dump analysis
	// contains all thread information, system information etc.
	[Serializable]
	public class SDResult : ISerializableJson {
		public SDSystemContext SystemContext { get; set; }
		public Dictionary<uint, SDThread> ThreadInformation { get; set; }
		public List<SDDeadlockContext> DeadlockInformation { get; set; }
		public List<SDBlockingObject> BlockingObjects { get; set; } 
		public Dictionary<ulong, SDMemoryObject> MemoryInformation { get; set; }

		public SDResult() {

		}
		public SDResult(SDSystemContext context, 
						Dictionary<uint, SDThread> threads, 
						Dictionary<ulong, SDMemoryObject> memoryObjects,
						List<SDDeadlockContext> deadlocks) {
			this.SystemContext = context;
			this.ThreadInformation = threads;
			this.MemoryInformation = memoryObjects;
			this.DeadlockInformation = deadlocks;
		}

		public void WriteResultToJSONFile(string file) {
			string json = this.SerializeToJSON();
			File.WriteAllText(file, json);

			SDResult test = JsonConvert.DeserializeObject<SDResult>(json);
		}
		public string SerializeToJSON() {
			return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings {
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			});
		}
	}
}
