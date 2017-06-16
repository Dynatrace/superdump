using SuperDump.Models;
using System.Collections.Generic;
using System.Linq;

namespace SuperDump.Analyzer.Linux.Analysis {
	/// <summary>
	/// This analyzer only cares about putting special "Tags" onto "ITaggableItems".
	/// E.g. mark agent frames, exceptions, ...
	/// </summary>
	public class TagAnalyzer {
		private readonly SDResult res;

		public TagAnalyzer(SDResult res) {
			this.res = res;
		}

		public void Analyze() {
			// modules
			foreach (var module in res.SystemContext.Modules) {
				if (SDTag.ContainsAgentName(module.FileName)) {
					module.Tags.Add(SDTag.DynatraceAgentTag);
				}
			}

			// threads
			foreach (var thread in res.ThreadInformation.Values) {
				// stackframes
				foreach (var frame in thread.StackTrace) {
					if (IsNativeExceptionMethodFrame(frame)) {
						frame.Tags.Add(SDTag.NativeExceptionTag);
						thread.Tags.Add(SDTag.NativeExceptionTag);
					}
					if (IsExceptionFrame(frame)) {
						frame.Tags.Add(SDTag.ExceptionInStackTag);
						thread.Tags.Add(SDTag.ExceptionInStackTag);
					}
					if (ContainsAny(frame.MethodName, "Thread::WaitSuspendEvents")) {
						frame.Tags.Add(SDTag.ClrThreadSuspend);
						thread.Tags.Add(SDTag.ClrThreadSuspend);
					}
					if (ContainsAny(frame.MethodName, "GCHeap::WaitUntilGCComplete") || ContainsAny(frame.MethodName, "SVR::gc_heap::wait_for_gc_done")) {
						frame.Tags.Add(SDTag.ClrWaitForGc);
						thread.Tags.Add(SDTag.ClrWaitForGc);
					}
					if (ContainsAny(frame.MethodName, "gc_heap::gc_thread_stub")) {
						frame.Tags.Add(SDTag.ClrGcThread);
						thread.Tags.Add(SDTag.ClrGcThread);
					}
					if (ContainsAny(frame.MethodName, "_CrtDbgReport")) {
						frame.Tags.Add(SDTag.AssertionErrorTag);
						thread.Tags.Add(SDTag.AssertionErrorTag);
					}
					if (IsDynatraceAgentFrame(frame)) {
						frame.Tags.Add(SDTag.DynatraceAgentTag);
						thread.Tags.Add(SDTag.DynatraceAgentTag);
					}
				}

				// managed exception
				if (thread.LastException != null) {
					thread.Tags.Add(SDTag.ManagedExceptionTag);
				}
			}

			// last executing
			if (res.ThreadInformation.ContainsKey(res.LastExecutedThread)) {
				res.ThreadInformation[res.LastExecutedThread].Tags.Add(SDTag.LastExecutingTag);
			}

			// last event
			if (res.LastEvent?.Description?.StartsWith("CLR exception") ?? false) {
				res.ThreadInformation.Values.Single(t => t.EngineId == res.LastEvent.ThreadId).Tags.Add(SDTag.ManagedExceptionTag);
			} else if (res.LastEvent?.Description?.StartsWith("Access violation") ?? false) {
				res.ThreadInformation.Values.Single(t => t.EngineId == res.LastEvent.ThreadId).Tags.Add(SDTag.NativeExceptionTag);
			} else if (res.LastEvent?.Description?.StartsWith("Break instruction exception") ?? false) {
				res.ThreadInformation.Values.Single(t => t.EngineId == res.LastEvent.ThreadId).Tags.Add(SDTag.BreakInstructionTag);
			}
		}

		private bool IsNativeExceptionMethodFrame(SDCombinedStackFrame frame) {
			return frame.ModuleName.StartsWith("libc") && ContainsAny(frame.MethodName, "gsignal", "abort", "raise");
		}

		private bool ContainsAny(string stringToSearch, params string[] keys) {
			string stringToSearchLower = stringToSearch.ToLower();
			foreach (var key in keys) {
				if (stringToSearchLower.Contains(key.ToLower())) {
					return true;
				}
			}
			return false;
		}

		private bool IsDynatraceAgentFrame(SDCombinedStackFrame frame) {
			return ContainsAny(frame.ModuleName + frame.Type.ToString() + frame.MethodName,
				"oneagent",
				"dtagent",
				"ruxitagent",
				"dtiisagent",
				"dtapacheagent"
				);
		}

		private bool IsExceptionFrame(SDCombinedStackFrame frame) {
			return ContainsAny(frame.MethodName, "exception");
		}
	}
}
