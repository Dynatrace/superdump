using SuperDump.Models;
using System;

namespace SuperDump.Analyzer.Common {
	public class DynatraceTagAnalyzer : DynamicAnalyzer {
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
				bool hasAddedSpecialAgentTag = false;
				hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, IsPhpAgentFrame, SDTag.DynatracePhpAgentTag);
				hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, IsJavaAgentFrame, SDTag.DynatraceJavaAgentTag);
				hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, IsDotnetAgentFrame, SDTag.DynatraceDotNetAgentTag);
				hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, IsProcessAgentFrame, SDTag.DynatraceProcessAgentTag);
				hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, IsIisAgentFrame, SDTag.DynatraceIisAgentTag);
				hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, IsLogAgentFrame, SDTag.DynatraceLogAgentTag);
				hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, IsOsAgentFrame, SDTag.DynatraceOsAgentTag);
				hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, IsPluginAgentFrame, SDTag.DynatracePluginAgentTag);
				hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, IsNetworkAgentFrame, SDTag.DynatraceNetworkAgentTag);
				hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, IsNginxAgentFrame, SDTag.DynatraceNginxAgentTag);
				hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, IsVarnishAgentFrame, SDTag.DynatraceVarnishAgentTag);
				hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, IsWatchdogFrame, SDTag.DynatraceWatchdogTag);
				hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, IsNodeAgentFrame, SDTag.DynatraceNodeAgentTag);

				if (!hasAddedSpecialAgentTag) {
					frame.Tags.Add(SDTag.DynatraceAgentTag);
					thread.Tags.Add(SDTag.DynatraceAgentTag);
				}
			}
		}

		private bool IsDynatraceAgentFrame(SDCombinedStackFrame frame) {
			return FrameContainsName(frame,
				"oneagent",
				"dtagent",
				"ruxitagent",
				"dtiisagent",
				"dtapacheagent"
				);
		}

		private bool IsPhpAgentFrame(SDCombinedStackFrame frame) =>	FrameContainsName(frame, "oneagentphp");
		private bool IsJavaAgentFrame(SDCombinedStackFrame frame) => FrameContainsName(frame, "oneagentjava");
		private bool IsDotnetAgentFrame(SDCombinedStackFrame frame) => FrameContainsName(frame, "oneagentdotnet");
		private bool IsProcessAgentFrame(SDCombinedStackFrame frame) => FrameContainsName(frame, "oneagentproc");
		private bool IsIisAgentFrame(SDCombinedStackFrame frame) => FrameContainsName(frame, "oneagentiis");
		private bool IsLogAgentFrame(SDCombinedStackFrame frame) => FrameContainsName(frame, "oneagentloganalytics");
		private bool IsOsAgentFrame(SDCombinedStackFrame frame) => FrameContainsName(frame, "oneagentos");
		private bool IsPluginAgentFrame(SDCombinedStackFrame frame) => FrameContainsName(frame, "oneagentplugin");
		private bool IsNetworkAgentFrame(SDCombinedStackFrame frame) => FrameContainsName(frame, "oneagentnetwork");
		private bool IsNginxAgentFrame(SDCombinedStackFrame frame) => FrameContainsName(frame, "oneagentnginx");
		private bool IsVarnishAgentFrame(SDCombinedStackFrame frame) => FrameContainsName(frame, "oneagentvarnish");
		private bool IsWatchdogFrame(SDCombinedStackFrame frame) => FrameContainsName(frame, "oneagentwatchdog");
		private bool IsNodeAgentFrame(SDCombinedStackFrame frame) => FrameContainsName(frame, "nodejsagent");
		private bool IsExceptionFrame(SDCombinedStackFrame frame) => ContainsAny(frame.MethodName, "exception");

		/// <summary>
		/// return true, if frame name contains any of the <paramref name="names"/>
		/// </summary>
		private bool FrameContainsName(SDCombinedStackFrame frame, params string[] names) {
			return ContainsAny(frame.ModuleName + frame.Type.ToString() + frame.MethodName, names);
		}

		/// <summary>
		/// if <paramref name="func"/> returns true, set <paramref name="tag"/> on frame and thread
		/// </summary>
		private bool AddTagIfFrameContains(SDThread thread, SDCombinedStackFrame frame, Func<SDCombinedStackFrame, bool> func, SDTag tag) {
			if (!func(frame)) return false;

			frame.Tags.Add(tag);
			thread.Tags.Add(tag);
			return true;
		}
	}
}
