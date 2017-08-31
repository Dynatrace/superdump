using SuperDump.Models;

namespace SuperDump.Analyzer.Common
{
	public class DynatraceTagAnalyzer : DynamicAnalyzer
	{
		public override void AnalyzeModule(SDModule module) {
			if (SDTag.ContainsAgentName(module.FileName)) {
				module.Tags.Add(SDTag.DynatraceAgentTag);
			}
		}

		public override void AnalyzeFrame(SDThread thread, SDCombinedStackFrame frame) {
			if (frame.MethodName == null) {
				return;
			}
			if (IsExceptionFrame(frame)) {
				frame.Tags.Add(SDTag.ExceptionInStackTag);
				thread.Tags.Add(SDTag.ExceptionInStackTag);
			}
			if (IsDynatraceAgentFrame(frame)) {
				if (IsPhpAgentFrame(frame)) {
					frame.Tags.Add(SDTag.DynatracePhpAgentTag);
					thread.Tags.Add(SDTag.DynatracePhpAgentTag);
				}
				if (IsJavaAgentFrame(frame)) {
					frame.Tags.Add(SDTag.DynatraceJavaAgentTag);
					thread.Tags.Add(SDTag.DynatraceJavaAgentTag);
				}
				if (IsProcessAgentFrame(frame)) {
					frame.Tags.Add(SDTag.DynatraceProcessAgentTag);
					thread.Tags.Add(SDTag.DynatraceProcessAgentTag);
				}
				if (IsIisAgentFrame(frame)) {
					frame.Tags.Add(SDTag.DynatraceIisAgentTag);
					thread.Tags.Add(SDTag.DynatraceIisAgentTag);
				}
				if (IsLogAgentFrame(frame)) {
					frame.Tags.Add(SDTag.DynatraceLogAgentTag);
					thread.Tags.Add(SDTag.DynatraceLogAgentTag);
				}
				if (IsOsAgentFrame(frame)) {
					frame.Tags.Add(SDTag.DynatraceOsAgentTag);
					thread.Tags.Add(SDTag.DynatraceOsAgentTag);
				}
				if (IsPluginAgentFrame(frame)) {
					frame.Tags.Add(SDTag.DynatracePluginAgentTag);
					thread.Tags.Add(SDTag.DynatracePluginAgentTag);
				}
				if (IsNetworkAgentFrame(frame)) {
					frame.Tags.Add(SDTag.DynatraceNetworkAgentTag);
					thread.Tags.Add(SDTag.DynatraceNetworkAgentTag);
				}
				if (IsNginxAgentFrame(frame)) {
					frame.Tags.Add(SDTag.DynatraceNginxAgentTag);
					thread.Tags.Add(SDTag.DynatraceNginxAgentTag);
				}
				if (IsVarnishAgentFrame(frame)) {
					frame.Tags.Add(SDTag.DynatraceVarnishAgentTag);
					thread.Tags.Add(SDTag.DynatraceVarnishAgentTag);
				}
				if (IsWatchdogFrame(frame)) {
					frame.Tags.Add(SDTag.DynatraceWatchdogTag);
					thread.Tags.Add(SDTag.DynatraceWatchdogTag);
				}
				if (IsNodeAgentFrame(frame)) {
					frame.Tags.Add(SDTag.DynatraceNodeAgentTag);
					thread.Tags.Add(SDTag.DynatraceNodeAgentTag);
				} else {
					frame.Tags.Add(SDTag.DynatraceAgentTag);
					thread.Tags.Add(SDTag.DynatraceAgentTag);
				}
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

		private bool IsPhpAgentFrame(SDCombinedStackFrame frame) {
			return ContainsAny(frame.ModuleName + frame.Type.ToString() + frame.MethodName,
				"oneagentphp"
				);
		}

		private bool IsJavaAgentFrame(SDCombinedStackFrame frame) {
			return ContainsAny(frame.ModuleName + frame.Type.ToString() + frame.MethodName,
				"oneagentjava"
				);
		}

		private bool IsProcessAgentFrame(SDCombinedStackFrame frame) {
			return ContainsAny(frame.ModuleName + frame.Type.ToString() + frame.MethodName,
				"oneagentproc"
				);
		}

		private bool IsIisAgentFrame(SDCombinedStackFrame frame) {
			return ContainsAny(frame.ModuleName + frame.Type.ToString() + frame.MethodName,
				"oneagentiis"
				);
		}

		private bool IsLogAgentFrame(SDCombinedStackFrame frame) {
			return ContainsAny(frame.ModuleName + frame.Type.ToString() + frame.MethodName,
				"oneagentloganalytics"
				);
		}

		private bool IsOsAgentFrame(SDCombinedStackFrame frame) {
			return ContainsAny(frame.ModuleName + frame.Type.ToString() + frame.MethodName,
				"oneagentos"
				);
		}

		private bool IsPluginAgentFrame(SDCombinedStackFrame frame) {
			return ContainsAny(frame.ModuleName + frame.Type.ToString() + frame.MethodName,
				"oneagentplugin"
				);
		}

		private bool IsNetworkAgentFrame(SDCombinedStackFrame frame) {
			return ContainsAny(frame.ModuleName + frame.Type.ToString() + frame.MethodName,
				"oneagentnetwork"
				);
		}

		private bool IsNginxAgentFrame(SDCombinedStackFrame frame) {
			return ContainsAny(frame.ModuleName + frame.Type.ToString() + frame.MethodName,
				"oneagentnginx"
				);
		}

		private bool IsVarnishAgentFrame(SDCombinedStackFrame frame) {
			return ContainsAny(frame.ModuleName + frame.Type.ToString() + frame.MethodName,
				"oneagentvarnish"
				);
		}

		private bool IsWatchdogFrame(SDCombinedStackFrame frame) {
			return ContainsAny(frame.ModuleName + frame.Type.ToString() + frame.MethodName,
				"oneagentwatchdog"
				);
		}

		private bool IsNodeAgentFrame(SDCombinedStackFrame frame) {
			return ContainsAny(frame.ModuleName + frame.Type.ToString() + frame.MethodName,
				"nodejsagent"
				);
		}

		private bool IsExceptionFrame(SDCombinedStackFrame frame) {
			return ContainsAny(frame.MethodName, "exception");
		}
	}
}
