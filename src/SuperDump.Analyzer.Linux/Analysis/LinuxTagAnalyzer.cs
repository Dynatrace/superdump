using SuperDump.Analyzer.Common;
using SuperDump.Models;
using System.Linq;

namespace SuperDump.Analyzer.Linux.Analysis {
	public class LinuxTagAnalyzer : DynamicAnalyzer {
		public override void AnalyzeResult(SDResult result) {
			// treat every linux signal as "native exception" for now.
			if (result.LastEvent != null) {
				result.ThreadInformation.Values.Single(t => t.EngineId == result.LastEvent.ThreadId).Tags.Add(SDTag.NativeExceptionTag);
			}
		}

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
