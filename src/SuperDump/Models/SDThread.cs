using Microsoft.Diagnostics.Runtime;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperDump.Models {
	[Serializable]
	public class SDThread : IEquatable<SDThread>, ISerializableJson {
		public uint Index { get; set; }
		public uint EngineId { get; set; }
		public uint OsId { get; set; }
		public bool IsManagedThread { get; set; }

		public SDCombinedStackTrace StackTrace { get; set; }
		public SDClrException LastException { get; set; }

		public IList<SDBlockingObject> BlockingObjects { get; set; } = new List<SDBlockingObject>();

		public ulong CreationTime { get; set; }
		public ulong ExitTime { get; set; }
		public ulong KernelTime { get; set; }
		public ulong UserTime { get; set; }
		public ulong StartOffset { get; set; }

		public uint ExitStatus { get; set; }
		public uint Priority { get; set; }
		public uint PriorityClass { get; set; }

		public SDThread() {

		}
		public SDThread(uint index) {
			this.Index = index;
		}

		public bool Equals(SDThread other) {
			bool equals = false;
			if(this.EngineId.Equals(other.EngineId) 
				&& this.OsId.Equals(other.OsId) 
				&& this.Index.Equals(other.Index) 
				&& this.CreationTime.Equals(other.CreationTime)
				&& this.ExitStatus.Equals(other.ExitStatus) 
				&& this.ExitTime.Equals(other.ExitTime) 
				&& this.IsManagedThread.Equals(other.IsManagedThread) 
				&& this.Priority.Equals(other.Priority)
				&& this.PriorityClass.Equals(other.PriorityClass)
				&& this.StartOffset.Equals(other.StartOffset)
				&& this.UserTime.Equals(other.UserTime)
				&& this.StackTrace.SequenceEqual(other.StackTrace)) {

				if (this.LastException == null && other.LastException == null)
					equals = true;
				else if (this.LastException == null && other.LastException != null)
					equals = false;
				else if (this.LastException.Equals(other.LastException))
					equals = true;
				else
					equals = false;
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
