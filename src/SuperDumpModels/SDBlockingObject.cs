using Microsoft.Diagnostics.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SuperDump.Models {
	public enum UnifiedBlockingReason {
		// managed blocking reasons
		None = 0,
		Unknown = 1,
		Monitor = 2,
		MonitorWait = 3,
		WaitOne = 4,
		WaitAll = 5,
		WaitAny = 6,
		ThreadJoin = 7,
		ReaderAcquired = 8,
		WriterAcquired = 9,

		// WCT_OBJECT_TYPE and handle types (for the future ;-) )
		CriticalSection = 10,
		SendMessage = 11,
		Mutex = 12,
		Alpc = 13,
		Com = 14,
		ThreadWait = 15,
		ProcessWait = 16,
		Thread = 17,
		ComActivation = 18,
		UnknownType = Unknown,
		File = 19,
		Job = 20,
		Semaphore = 21,
		Event = 22,
		Timer = 23,
		MemorySection = 24
	}

	[Serializable]
	public class SDBlockingObject : ISerializableJson {
		[JsonConverter(typeof(StringEnumConverter))]
		public UnifiedBlockingReason Reason { get; set; }
		public bool HasOwnershipInformation => OwnerOSThreadIds.Count > 0;
		public List<uint> OwnerOSThreadIds { get; set; } = new List<uint>();
		public List<uint> WaiterOSThreadIds { get; set; } = new List<uint>();
		public int RecursionCount { get; set; }
		public ulong ManagedObjectAddress { get; set; }

		public SDBlockingObject() { }

		public SDBlockingObject(BlockingObject obj) {
			foreach (ClrThread waiter in obj.Waiters?.Where(t => t != null) ?? new ClrThread[0]) {
				this.WaiterOSThreadIds.Add(waiter.OSThreadId);
			}
			foreach (ClrThread owner in obj.Owners?.Where(t => t != null) ?? new ClrThread[0]) {
				this.OwnerOSThreadIds.Add(owner.OSThreadId);
			}

			// convert reason to int, so it can be case to our UnifiedBlockingReason
			Reason = (UnifiedBlockingReason)((int)obj.Reason);
			RecursionCount = obj.RecursionCount;
			ManagedObjectAddress = obj.Object;
		}

		public string SerializeToJSON() {
			return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings {
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			});
		}
	}
}
