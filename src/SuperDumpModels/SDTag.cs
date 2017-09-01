using System;

namespace SuperDump.Models {
	public class SDTag : IEquatable<SDTag> {
		public string Name { get; set; }

		/// <summary>
		/// importance has relevance on sorting in the UI. e.g. ranking of threads (higher is better)
		/// </summary>
		public int Importance { get; set; }

		public SDTag(string name, int importance = 0) {
			this.Name = name;
			this.Importance = importance;
		}

		public override string ToString() {
			return Name;
		}

		public override bool Equals(object obj) {
			return Equals(obj as SDTag);
		}

		public bool Equals(SDTag other) {
			if (other == null) return false;
			return other.Name == this.Name;
		}

		public override int GetHashCode() {
			return this.Name?.GetHashCode() ?? 0;
		}

		// all tags must be css-class compatible (don't use spaces!)
		public static readonly SDTag LastExecutingTag = new SDTag("last-executing", 100);
		public static readonly SDTag DeadlockedTag = new SDTag("deadlocked", 90);
		public static readonly SDTag NativeExceptionTag = new SDTag("native-exception", 81);
		public static readonly SDTag ManagedExceptionTag = new SDTag("managed-exception", 80);
		public static readonly SDTag AssertionErrorTag = new SDTag("assertion-error", 80);
		public static readonly SDTag ExceptionInStackTag = new SDTag("exception-in-stack", 70);
		public static readonly SDTag ClrThreadSuspend = new SDTag("clr-thread-suspend", 60);
		public static readonly SDTag DynatraceAgentTag = new SDTag("dynatrace-agent", 10);
		public static readonly SDTag DynatraceJavaAgentTag = new SDTag("dynatrace-agent-java", 10);
		public static readonly SDTag DynatraceDotNetAgentTag = new SDTag("dynatrace-agent-dotnet", 10);
		public static readonly SDTag DynatraceIisAgentTag = new SDTag("dynatrace-agent-iis", 10);
		public static readonly SDTag DynatraceNodeAgentTag = new SDTag("dynatrace-agent-node", 10);
		public static readonly SDTag DynatracePhpAgentTag = new SDTag("dynatrace-agent-php", 10);
		public static readonly SDTag DynatraceProcessAgentTag = new SDTag("dynatrace-agent-process", 10);
		public static readonly SDTag DynatraceLogAgentTag = new SDTag("dynatrace-agent-log", 10);
		public static readonly SDTag DynatraceOsAgentTag = new SDTag("dynatrace-agent-os", 10);
		public static readonly SDTag DynatracePluginAgentTag = new SDTag("dynatrace-agent-plugin", 10);
		public static readonly SDTag DynatraceNetworkAgentTag = new SDTag("dynatrace-agent-network", 10);
		public static readonly SDTag DynatraceNginxAgentTag = new SDTag("dynatrace-agent-nginx", 10);
		public static readonly SDTag DynatraceVarnishAgentTag = new SDTag("dynatrace-agent-varnish", 10);
		public static readonly SDTag DynatraceWatchdogTag = new SDTag("dynatrace-agent-watchdog", 10);
		public static readonly SDTag DynatraceAgentLoaderTag = new SDTag("dynatrace-agent-loader", 10);
		public static readonly SDTag ClrWaitForGc = new SDTag("clr-wait-for-gc", 0);
		public static readonly SDTag ClrGcThread = new SDTag("clr-gc-thread", 0);
		public static readonly SDTag BreakInstructionTag = new SDTag("break-instruction", 0);
	}
}
