using SuperDump.Models;
using System;

namespace SuperDump.Analyzer.Common {
	public class DynatraceTagAnalyzer : DynamicAnalyzer {
		public override void AnalyzeModule(SDModule module) {
			bool hasAddedSpecialAgentTag = false;
			hasAddedSpecialAgentTag |= AddTagIfModuleContains(module, ContainsPhpAgentModule, SDTag.DynatracePhpAgentTag);
			hasAddedSpecialAgentTag |= AddTagIfModuleContains(module, ContainsJavaAgentModule, SDTag.DynatraceJavaAgentTag);
			hasAddedSpecialAgentTag |= AddTagIfModuleContains(module, ContainsDotnetAgentModule, SDTag.DynatraceDotNetAgentTag);
			hasAddedSpecialAgentTag |= AddTagIfModuleContains(module, ContainsProcessAgentModule, SDTag.DynatraceProcessAgentTag);
			hasAddedSpecialAgentTag |= AddTagIfModuleContains(module, ContainsIisAgentModule, SDTag.DynatraceIisAgentTag);
			hasAddedSpecialAgentTag |= AddTagIfModuleContains(module, ContainsLogAgentModule, SDTag.DynatraceLogAgentTag);
			hasAddedSpecialAgentTag |= AddTagIfModuleContains(module, ContainsOsAgentModule, SDTag.DynatraceOsAgentTag);
			hasAddedSpecialAgentTag |= AddTagIfModuleContains(module, ContainsPluginAgentModule, SDTag.DynatracePluginAgentTag);
			hasAddedSpecialAgentTag |= AddTagIfModuleContains(module, ContainsNetworkAgentModule, SDTag.DynatraceNetworkAgentTag);
			hasAddedSpecialAgentTag |= AddTagIfModuleContains(module, ContainsNginxAgentModule, SDTag.DynatraceNginxAgentTag);
			hasAddedSpecialAgentTag |= AddTagIfModuleContains(module, ContainsVarnishAgentModule, SDTag.DynatraceVarnishAgentTag);
			hasAddedSpecialAgentTag |= AddTagIfModuleContains(module, ContainsWatchdogFrame, SDTag.DynatraceWatchdogTag);
			hasAddedSpecialAgentTag |= AddTagIfModuleContains(module, ContainsNodeAgentModule, SDTag.DynatraceNodeAgentTag);
			hasAddedSpecialAgentTag |= AddTagIfModuleContains(module, ContainsAgentLoaderModule, SDTag.DynatraceAgentLoaderTag);

			if (!hasAddedSpecialAgentTag) {
				// if no special agent has been detected, add generic dynatrace agent tag
				AddTagIfModuleContains(module, ContainsDynatraceModule, SDTag.DynatraceAgentTag);
			}
		}

		/// <summary>
		/// if <paramref name="func"/> returns true, set <paramref name="tag"/> on frame and thread
		/// </summary>
		private bool AddTagIfModuleContains(SDModule module, Func<string, bool> func, SDTag tag) {
			if (!func(module.FileName)) return false;

			module.Tags.Add(tag);
			return true;
		}

		public override void AnalyzeFrame(SDThread thread, SDCombinedStackFrame frame) {
			if (frame.MethodName == null) {
				return;
			}

			AddTagIfFrameContains(thread, frame, x => ContainsAny(frame.MethodName, "exception"), SDTag.ExceptionInStackTag);

			bool hasAddedSpecialAgentTag = false;
			hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, ContainsPhpAgentModule, SDTag.DynatracePhpAgentTag);
			hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, ContainsJavaAgentModule, SDTag.DynatraceJavaAgentTag);
			hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, ContainsDotnetAgentModule, SDTag.DynatraceDotNetAgentTag);
			hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, ContainsProcessAgentModule, SDTag.DynatraceProcessAgentTag);
			hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, ContainsIisAgentModule, SDTag.DynatraceIisAgentTag);
			hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, ContainsLogAgentModule, SDTag.DynatraceLogAgentTag);
			hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, ContainsOsAgentModule, SDTag.DynatraceOsAgentTag);
			hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, ContainsPluginAgentModule, SDTag.DynatracePluginAgentTag);
			hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, ContainsNetworkAgentModule, SDTag.DynatraceNetworkAgentTag);
			hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, ContainsNginxAgentModule, SDTag.DynatraceNginxAgentTag);
			hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, ContainsVarnishAgentModule, SDTag.DynatraceVarnishAgentTag);
			hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, ContainsWatchdogFrame, SDTag.DynatraceWatchdogTag);
			hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, ContainsNodeAgentModule, SDTag.DynatraceNodeAgentTag);
			hasAddedSpecialAgentTag |= AddTagIfFrameContains(thread, frame, ContainsAgentLoaderModule, SDTag.DynatraceAgentLoaderTag);

			if (!hasAddedSpecialAgentTag) {
				// if no special agent has been detected, add generic dynatrace agent tag
				AddTagIfFrameContains(thread, frame, ContainsDynatraceModule, SDTag.DynatraceAgentTag);
			}
		}

		private bool ContainsDynatraceModule(string str) {
			return ContainsAny(str,
				"oneagent",
				"dtagent",
				"ruxitagent",
				"dtiisagent",
				"dtapacheagent"
				);
		}

		private bool ContainsPhpAgentModule(string str) => ContainsAny(str, "oneagentphp");
		private bool ContainsJavaAgentModule(string str) => ContainsAny(str, "oneagentjava");
		private bool ContainsDotnetAgentModule(string str) => ContainsAny(str, "oneagentdotnet");
		private bool ContainsProcessAgentModule(string str) => ContainsAny(str, "oneagentproc");
		private bool ContainsIisAgentModule(string str) => ContainsAny(str, "oneagentiis");
		private bool ContainsLogAgentModule(string str) => ContainsAny(str, "oneagentloganalytics");
		private bool ContainsOsAgentModule(string str) => ContainsAny(str, "oneagentos");
		private bool ContainsPluginAgentModule(string str) => ContainsAny(str, "oneagentplugin");
		private bool ContainsNetworkAgentModule(string str) => ContainsAny(str, "oneagentnetwork");
		private bool ContainsNginxAgentModule(string str) => ContainsAny(str, "oneagentnginx");
		private bool ContainsVarnishAgentModule(string str) => ContainsAny(str, "oneagentvarnish");
		private bool ContainsWatchdogFrame(string str) => ContainsAny(str, "oneagentwatchdog");
		private bool ContainsNodeAgentModule(string str) => ContainsAny(str, "nodejsagent");
		private bool ContainsAgentLoaderModule(string str) => ContainsAny(str, "oneagentloader");

		/// <summary>
		/// return true, if frame name contains any of the <paramref name="names"/>
		/// </summary>
		private bool FrameContainsName(SDCombinedStackFrame frame, params string[] names) {
			return ContainsAny(frame.ModuleName + frame.Type.ToString() + frame.MethodName, names);
		}

		/// <summary>
		/// if <paramref name="func"/> returns true, set <paramref name="tag"/> on frame and thread
		/// </summary>
		private bool AddTagIfFrameContains(SDThread thread, SDCombinedStackFrame frame, Func<string, bool> func, SDTag tag) {
			if (!func(frame.ModuleName + frame.Type.ToString() + frame.MethodName)) return false;

			frame.Tags.Add(tag);
			thread.Tags.Add(tag);
			return true;
		}
	}
}
