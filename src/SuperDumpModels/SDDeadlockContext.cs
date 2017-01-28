using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SuperDump.Models {
	/// <summary>
	/// represents information about a deadlock, that means locked thread
	/// and objects that are locked on
	/// </summary>

	[Serializable]
	public class SDDeadlockContext : IEquatable<SDDeadlockContext>, ISerializableJson {
		public HashSet<uint> pathToDeadlock = new HashSet<uint>();
		public uint lastThreadId;
		public uint lockedOnThreadId;

		public SDDeadlockContext() { }

		public SDDeadlockContext(HashSet<uint> visitedThreads, uint lastThread, uint lockedOnThread) {
			this.pathToDeadlock = visitedThreads;
			this.lastThreadId = lastThread;
			this.lockedOnThreadId = lockedOnThread;
		}

		public string SerializeToJSON() {
			return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings {
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			});
		}

		public override bool Equals(object obj) {
			if (obj is SDDeadlockContext) {
				var context = obj as SDDeadlockContext;
				return this.Equals(context);
			} else {
				return false;
			}
		}

		public override int GetHashCode() {
			return base.GetHashCode();
		}

		public bool Equals(SDDeadlockContext other) {
			bool equals = false;
			if (this.pathToDeadlock.SetEquals(other.pathToDeadlock)
				&& this.lastThreadId.Equals(other.lastThreadId)
				&& this.lockedOnThreadId.Equals(other.lockedOnThreadId)) {
				equals = true;
			}
			return equals;
		}
	}
}
