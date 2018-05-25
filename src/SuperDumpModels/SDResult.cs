using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SuperDump.Models {
	// SDResult identifies the whole result of the dump analysis
	// contains all thread information, system information etc.
	public class SDResult : ISerializableJson {
		public bool IsManagedProcess { get; set; }
		public uint LastExecutedThread { get; set; }
		public DumpInfo AnalysisInfo { get; set; } = new DumpInfo();
		public SDSystemContext SystemContext { get; set; }
		public SDLastEvent LastEvent { get; set; }
		public List<SDClrException> ExceptionRecord { get; set; }
		public Dictionary<uint, SDThread> ThreadInformation { get; set; }
		public List<SDDeadlockContext> DeadlockInformation { get; set; }
		public List<SDBlockingObject> BlockingObjects { get; set; }
		public Dictionary<ulong, SDMemoryObject> MemoryInformation { get; set; }
		public List<string> NotLoadedSymbols { get; set; }

		public SDResult() { }

		public SDResult(SDSystemContext context,
						SDLastEvent lastEvent,
						List<SDClrException> exceptions,
						Dictionary<uint, SDThread> threads,
						Dictionary<ulong, SDMemoryObject> memoryObjects,
						List<SDDeadlockContext> deadlocks,
						List<string> notLoadedSymbols) {
			this.SystemContext = context;
			this.LastEvent = lastEvent;
			this.ExceptionRecord = exceptions;
			this.ThreadInformation = threads;
			this.MemoryInformation = memoryObjects;
			this.DeadlockInformation = deadlocks;
			this.NotLoadedSymbols = notLoadedSymbols;
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

		public ISet<SDTag> GetThreadTags() {
			var tags = new HashSet<SDTag>();
			if (ThreadInformation == null) return tags;
			foreach (var thread in ThreadInformation) {
				if (thread.Value.Tags == null) continue;
				foreach (var tag in thread.Value.Tags) {
					tags.Add(tag);
				}
			}
			return tags;
		}

		/// <summary>
		/// Returns the thread with the most severe error-tag on it. "most likely" the crashing thread. might also return null, if there is no thread with error tags.
		/// </summary>
		public SDThread GetErrorThread() {
			// order threads by importance of their error-tags, then return first
			return ThreadInformation.Values.OrderByDescending(t => t.ErrorTags.Any() ? t.ErrorTags.Max(x => x.Importance) : 0).FirstOrDefault();
		}
	}
}
