using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;
using SuperDump.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Newtonsoft.Json;

namespace SuperDump {
	/// <summary>
	/// Represents a combined stack trace, which can consist of native and managed frames
	/// </summary>
	[Serializable]
	public class CombinedStackTrace : ICollection<CombinedStackFrame>, ISerializableJson {
		[NonSerialized]
		private uint engineId;
		[NonSerialized]
		private uint osId;
		[NonSerialized]
		private bool isManaged;
		[NonSerialized]
		private IDebugClient debugClient;
		[NonSerialized]
		private DumpContext context;
		public IList<CombinedStackFrame> Trace { get; private set; }

		public int Count => Trace.Count;
		public bool IsReadOnly => Trace.IsReadOnly;

		/// <summary>
		/// Constructs a mixed stack trace, native and managed
		/// </summary>
		/// <param name="debugClient"></param>
		/// <param name="runtime"></param>
		public CombinedStackTrace(uint engineId, uint osId, bool isManaged, IDebugClient debugClient, DumpContext context) {
			this.engineId = engineId;
			this.osId = osId;
			this.isManaged = isManaged;
			this.debugClient = debugClient;
			this.context = context;

			this.Trace = this.GetCompleteStackTrace();
		}

		[JsonConstructor]
		public CombinedStackTrace(List<CombinedStackFrame> trace) {
			this.Trace = trace;
		}

		/// <summary>
		/// Helper struct to store the offsets of a DEBUG_STACK_FRAME
		/// </summary>
		private struct StackFrameOffset {
			public ulong frameOffset;
			public ulong stackOffset;
			public ulong instructionOffset;

			public void Update(ref DEBUG_STACK_FRAME frame) {
				frameOffset = frame.FrameOffset;
				stackOffset = frame.StackOffset;
				instructionOffset = frame.InstructionOffset;
			}
		}

		/// <summary>
		/// Gets the native frames of a thread's stack trace
		/// </summary>
		/// <param name="engineThreadId"></param>
		/// <returns></returns>
		private IList<CombinedStackFrame> GetNativeStackTrace(uint engineThreadId) {
			Utility.CheckHRESULT(((IDebugSystemObjects)this.debugClient).SetCurrentThreadId(engineThreadId));
			const int cBufferSize = 1000;
			var stackFrames = new DEBUG_STACK_FRAME[cBufferSize];
			uint framesFilled;

#if DEBUG
			/*((IDebugSymbols2)debugClient).SetSymbolPath(context.SymbolPath);
			StringBuilder name = new StringBuilder(500);
			uint size;
			((IDebugSymbols2)debugClient).GetSymbolPath(name, name.Capacity, out size);
			context.WriteLine("Path: " + name.ToString());*/
#endif
			//((IDebugSymbols)debugClient).SetSymbolOptions(SYMOPT.DEBUG);
			var debugControl = debugClient as IDebugControl;
			var stackTrace = new List<CombinedStackFrame>();

			uint startOffset = 0;
			StackFrameOffset pos = new StackFrameOffset();
			Utility.CheckHRESULT(debugControl.GetStackTrace(pos.frameOffset, pos.stackOffset, pos.instructionOffset, stackFrames, stackFrames.Length, out framesFilled));

			while (framesFilled > startOffset) {
				for (uint i = startOffset; i < framesFilled; i++) {
					stackTrace.Add(new CombinedStackFrame(stackFrames[i], (IDebugSymbols3)this.debugClient));
				}

				// update iterator with last frame
				pos.Update(ref stackFrames[framesFilled - 1]);
				startOffset = 1; // last frame is included at first position
				Utility.CheckHRESULT(debugControl.GetStackTrace(pos.frameOffset, pos.stackOffset, pos.instructionOffset, stackFrames, stackFrames.Length, out framesFilled));
			}

			return stackTrace;
		}

		/// <summary>
		/// Gets the managed stack trace of a managed thread
		/// </summary>
		/// <param name="thread"></param>
		/// <returns></returns>
		private IList<CombinedStackFrame> GetManagedStackTrace(uint osThreadId) {
			var stackTrace = new List<CombinedStackFrame>();
			if (this.context.Runtime != null) {
				ClrThread thread = this.context.Runtime.Threads.FirstOrDefault(t => t.OSThreadId == osThreadId);

				foreach (ClrStackFrame frame in thread.StackTrace) {
					stackTrace.Add(new CombinedStackFrame(frame));
				}
			}

			return stackTrace;
		}

		/// <summary>
		/// Gets a complete stack trace, native and also managed
		/// </summary>
		/// <param name="threadIndex"></param>
		/// <returns>A list with stack frames, where native and managed frames are linked</returns>
		public IList<CombinedStackFrame> GetCompleteStackTrace() {
			var unifiedStackTrace = new List<CombinedStackFrame>();

			// NOTE: with GetNativeStackTrace you get ALL frames, so also possible managed frames,
			// so the only thing left to do is, check if there is a frame in the managed call stack, 
			// which refers to the one from GetNativeStackTrace()!
			CombinedStackFrame[] nativeStackTrace = GetNativeStackTrace(this.engineId).OrderBy(x => x.StackPointer).ToArray();
			if (this.isManaged) {
				// get managed thread stack (like !clrstack)
				CombinedStackFrame[] managedStackTrace = GetManagedStackTrace(this.osId).OrderBy(x => x.StackPointer).ToArray();

				int idxNative = 0;
				int idxManaged = 0;
				while (idxNative < nativeStackTrace.Length || idxManaged < managedStackTrace.Length) {
					if (idxNative == nativeStackTrace.Length) {
						// native exhausted, add managed
						unifiedStackTrace.Add(managedStackTrace[idxManaged++]);
						continue;
					}
					if (idxManaged == managedStackTrace.Length) {
						// managed exhausted, add managed
						unifiedStackTrace.Add(nativeStackTrace[idxNative++]);
						continue;
					}
					if (managedStackTrace[idxManaged].StackPointer == nativeStackTrace[idxNative].StackPointer) {
						// IP's match. prefer managed frame
						unifiedStackTrace.Add(managedStackTrace[idxManaged++]);
						idxNative++; // skip over native frame
						continue;
					}
					if (managedStackTrace[idxManaged].StackPointer < nativeStackTrace[idxNative].StackPointer) {
						// managed SP's lower. go with it.
						unifiedStackTrace.Add(managedStackTrace[idxManaged++]);
						continue;
					} else {
						// native SP's lower. go with it.
						unifiedStackTrace.Add(nativeStackTrace[idxNative++]);
						continue;
					}
				}
			} else {
				return nativeStackTrace;
			}
			return unifiedStackTrace;
		}

		public IEnumerator<CombinedStackFrame> GetEnumerator() {
			return this.Trace.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return this.GetEnumerator();
		}

		public void Add(CombinedStackFrame item) {
			Trace.Add(item);
		}

		public void Clear() {
			Trace.Clear();
		}

		public bool Contains(CombinedStackFrame item) {
			return Trace.Contains(item);
		}

		public void CopyTo(CombinedStackFrame[] array, int arrayIndex) {
			Trace.CopyTo(array, arrayIndex);
		}

		public bool Remove(CombinedStackFrame item) {
			return Trace.Remove(item);
		}

		public CombinedStackFrame this[int index] {
			get { return this.Trace[index]; }
			set { this.Trace[index] = value; }
		}

		public string SerializeToJSON() {
			return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings {
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			});
		}
	}
}
