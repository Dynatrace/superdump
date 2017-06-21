using SuperDump.Models;

namespace SuperDump.Analyzer.Common {
	public class DotNetTagAnalyzer : DynamicAnalyzer {
		public override void AnalyzeThread(SDThread thread) {
			if (thread.LastException != null) {
				thread.Tags.Add(SDTag.ManagedExceptionTag);
			}
		}

		public override void AnalyzeFrame(SDThread thread, SDCombinedStackFrame frame) {
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
		}
	}
}
