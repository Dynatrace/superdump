using SuperDump.Analyzer.Common;
using SuperDump.Models;

namespace SuperDump.Analyzer.Linux.Analysis {
	public class LinuxTagAnalyzer : DynamicAnalyzer {
		public override void AnalyzeFrame(SDThread thread, SDCombinedStackFrame frame) {
			if (IsNativeExceptionMethodFrame(frame)) {
				frame.Tags.Add(SDTag.NativeExceptionTag);
				thread.Tags.Add(SDTag.NativeExceptionTag);
			}
		}

		private bool IsNativeExceptionMethodFrame(SDCombinedStackFrame frame) {
			return frame.ModuleName.StartsWith("libc") && ContainsAny(frame.MethodName, "gsignal", "abort", "raise");
		}
	}
}
