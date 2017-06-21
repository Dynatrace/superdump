using SuperDump.Models;

namespace SuperDump.Analyzer.Common {
	public class DynatraceTagAnalyzer : DynamicAnalyzer {
		public override void AnalyzeModule(SDModule module) {
			if (SDTag.ContainsAgentName(module.FileName)) {
				module.Tags.Add(SDTag.DynatraceAgentTag);
			}
		}

		public override void AnalyzeFrame(SDThread thread, SDCombinedStackFrame frame) {
			if (IsExceptionFrame(frame)) {
				frame.Tags.Add(SDTag.ExceptionInStackTag);
				thread.Tags.Add(SDTag.ExceptionInStackTag);
			}
			if (IsDynatraceAgentFrame(frame)) {
				frame.Tags.Add(SDTag.DynatraceAgentTag);
				thread.Tags.Add(SDTag.DynatraceAgentTag);
			}
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
