using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SuperDump.Models {
	public enum ThreadState {
		//TS_Unknown                = 0x00000000,    // threads are initialized this way

		TS_AbortRequested = 0x00000001,    // Abort the thread
		TS_GCSuspendPending = 0x00000002,    // waiting to get to safe spot for GC
		TS_UserSuspendPending = 0x00000004,    // user suspension at next opportunity
		TS_DebugSuspendPending = 0x00000008,    // Is the debugger suspending threads?
												//TS_GCOnTransitions        = 0x00000010,    // Force a GC on stub transitions (GCStress only)

		//TS_LegalToJoin            = 0x00000020,    // Is it now legal to attempt a Join()
		//TS_YieldRequested         = 0x00000040,    // The task should yield
		//TS_Hijacked               = 0x00000080,    // Return address has been hijacked
		//TS_BlockGCForSO           = 0x00000100,    // If a thread does not have enough stack, WaitUntilGCComplete may fail.
		// Either GC suspension will wait until the thread has cleared this bit,
		// Or the current thread is going to spin if GC has suspended all threads.
		TS_Background = 0x00000200,    // Thread is a background thread
		TS_Unstarted = 0x00000400,    // Thread has never been started
		TS_Dead = 0x00000800,    // Thread is dead

		//TS_WeOwn                  = 0x00001000,    // Exposed object initiated this thread
		TS_CoInitialized = 0x00002000,    // CoInitialize has been called for this thread

		TS_InSTA = 0x00004000,    // Thread hosts an STA
		TS_InMTA = 0x00008000,    // Thread is part of the MTA

		// Some bits that only have meaning for reporting the state to clients.
		//TS_ReportDead             = 0x00010000,    // in WaitForOtherThreads()

		//TS_TaskReset              = 0x00040000,    // The task is reset

		//TS_SyncSuspended          = 0x00080000,    // Suspended via WaitSuspendEvent
		//TS_DebugWillSync          = 0x00100000,    // Debugger will wait for this thread to sync

		//TS_StackCrawlNeeded       = 0x00200000,    // A stackcrawl is needed on this thread, such as for thread abort
		// See comment for s_pWaitForStackCrawlEvent for reason.

		//TS_SuspendUnstarted       = 0x00400000,    // latch a user suspension on an unstarted thread

		TS_Aborted = 0x00800000,    // is the thread aborted?
		TS_TPWorkerThread = 0x01000000,    // is this a threadpool worker thread?

		//TS_Interruptible          = 0x02000000,    // sitting in a Sleep(), Wait(), Join()
		//TS_Interrupted            = 0x04000000,    // was awakened by an interrupt APC. !!! This can be moved to TSNC

		TS_CompletionPortThread = 0x08000000,    // Completion port thread

		TS_AbortInitiated = 0x10000000,    // set when abort is begun

		//TS_Finalized              = 0x20000000,    // The associated managed Thread object has been finalized.
		// We can clean up the unmanaged part now.

		//TS_FailStarted            = 0x40000000,    // The thread fails during startup.
		//TS_Detached               = 0x80000000,    // Thread was detached by DllMain
	}

	public class SDThread : IEquatable<SDThread>, ISerializableJson, ITaggableItem {
		public uint Index { get; set; }
		public uint EngineId { get; set; }
		public uint OsId { get; set; }
		public int ManagedThreadId { get; set; }
		public bool IsManagedThread { get; set; }
		public string ThreadName { get; set; } = string.Empty;

		public SDCombinedStackTrace StackTrace { get; set; }
		public SDClrException LastException { get; set; }

		public IList<SDBlockingObject> BlockingObjects { get; set; } = new List<SDBlockingObject>();

		public ISet<SDTag> Tags { get; } = new HashSet<SDTag>();
		public IEnumerable<SDTag> ErrorTags => Tags.Where(x => x.Type == TagType.Error);

		public ulong CreationTime { get; set; }
		public ulong ExitTime { get; set; }
		public ulong KernelTime { get; set; }
		public ulong UserTime { get; set; }
		public ulong StartOffset { get; set; }

		public uint ExitStatus { get; set; }
		public uint Priority { get; set; }
		public uint PriorityClass { get; set; }

		public string State { get; set; }
		public bool IsThreadPoolThread { get; set; }

		public SDThread() { }

		public SDThread(uint index) {
			this.Index = index;
		}

		public bool Equals(SDThread other) {
			bool equals = false;
			if (this.EngineId.Equals(other.EngineId)
				&& this.OsId.Equals(other.OsId)
				&& this.ManagedThreadId.Equals(other.ManagedThreadId)
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
