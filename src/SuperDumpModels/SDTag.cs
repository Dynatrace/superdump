using System;

namespace SuperDump.Models {
	public class SDTag : IEquatable<SDTag> {
		public string Name { get; set; }

		/// <summary>
		/// importance has relevance on sorting in the UI. e.g. ranking of threads (higher is better)
		/// </summary>
		public int Importance { get; set; }

		public TagType Type { get; set; }

		public SDTag(string name, int importance, TagType type) {
			this.Name = name;
			this.Importance = importance;
			this.Type = type;
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

		// all tag-names must be css-class compatible (don't use spaces!)
		
		// type: error
		public static readonly SDTag AbortTag = new SDTag("abort", 100, TagType.Error);
		public static readonly SDTag PureCallTag = new SDTag("purecall", 100, TagType.Error);
		public static readonly SDTag StackOverflowTag = new SDTag("stackoverflow", 100, TagType.Error);
		public static readonly SDTag DeadlockedTag = new SDTag("deadlocked", 90, TagType.Error);
		public static readonly SDTag NativeExceptionTag = new SDTag("native-exception", 81, TagType.Error);
		public static readonly SDTag ManagedExceptionTag = new SDTag("managed-exception", 80, TagType.Error);
		public static readonly SDTag AssertionErrorTag = new SDTag("assertion-error", 80, TagType.Error);
		public static readonly SDTag BufferOverrunTag = new SDTag("buffer-overrun", 80, TagType.Error);
		public static readonly SDTag ExceptionCatchTag = new SDTag("exception-catch", 71, TagType.Error);
		public static readonly SDTag ExceptionInStackTag = new SDTag("exception-in-text", 70, TagType.Error);

		// type: info
		public static readonly SDTag LastExecutingTag = new SDTag("last-executing", 100, TagType.Info);
		public static readonly SDTag ClrThreadSuspend = new SDTag("clr-thread-suspend", 60, TagType.Info);
		public static readonly SDTag DynatraceAgentTag = new SDTag("dynatrace-agent", 10, TagType.Info);
		public static readonly SDTag DynatraceJavaAgentTag = new SDTag("dynatrace-agent-java", 10, TagType.Info);
		public static readonly SDTag DynatraceDotNetAgentTag = new SDTag("dynatrace-agent-dotnet", 10, TagType.Info);
		public static readonly SDTag DynatraceIisAgentTag = new SDTag("dynatrace-agent-iis", 10, TagType.Info);
		public static readonly SDTag DynatraceNodeAgentTag = new SDTag("dynatrace-agent-node", 10, TagType.Info);
		public static readonly SDTag DynatracePhpAgentTag = new SDTag("dynatrace-agent-php", 10, TagType.Info);
		public static readonly SDTag DynatraceProcessAgentTag = new SDTag("dynatrace-agent-process", 10, TagType.Info);
		public static readonly SDTag DynatraceLogAgentTag = new SDTag("dynatrace-agent-log", 10, TagType.Info);
		public static readonly SDTag DynatraceOsAgentTag = new SDTag("dynatrace-agent-os", 10, TagType.Info);
		public static readonly SDTag DynatracePluginAgentTag = new SDTag("dynatrace-agent-plugin", 10, TagType.Info);
		public static readonly SDTag DynatraceNetworkAgentTag = new SDTag("dynatrace-agent-network", 10, TagType.Info);
		public static readonly SDTag DynatraceNginxAgentTag = new SDTag("dynatrace-agent-nginx", 10, TagType.Info);
		public static readonly SDTag DynatraceVarnishAgentTag = new SDTag("dynatrace-agent-varnish", 10, TagType.Info);
		public static readonly SDTag DynatraceWatchdogTag = new SDTag("dynatrace-agent-watchdog", 10, TagType.Info);
		public static readonly SDTag DynatraceAgentLoaderTag = new SDTag("dynatrace-agent-loader", 10, TagType.Info);
		public static readonly SDTag ClrWaitForGc = new SDTag("clr-wait-for-gc", 0, TagType.Info);
		public static readonly SDTag ClrGcThread = new SDTag("clr-gc-thread", 0, TagType.Info);
		public static readonly SDTag BreakInstructionTag = new SDTag("break-instruction", 0, TagType.Info);
		
		/// <summary>
		/// When properties of tags change (TagType or Priority), this method helps to fix already persisted tags.
		/// E.g. when TagType was introduced, all existing tags were type==Undefined. Since, based on the tag-name it's now clear
		/// if it's an Error tag or not, we introduced this method to return the correct type.
		/// This could be done in a nicer way (dictionary).
		/// </summary>
		public static SDTag FixUpTagType(SDTag tag) {
			if (tag == AbortTag) return AbortTag;
			if (tag == PureCallTag) return PureCallTag;
			if (tag == StackOverflowTag) return StackOverflowTag;
			if (tag == DeadlockedTag) return DeadlockedTag;
			if (tag == NativeExceptionTag) return NativeExceptionTag;
			if (tag == ManagedExceptionTag) return ManagedExceptionTag;
			if (tag == AssertionErrorTag) return AssertionErrorTag;
			if (tag == BufferOverrunTag) return BufferOverrunTag;
			if (tag == ExceptionCatchTag) return ExceptionCatchTag;
			if (tag == ExceptionInStackTag) return ExceptionInStackTag;
			if (tag.Name == "exception-in-stack") return ExceptionInStackTag; // changed name

			return new SDTag(tag.Name, tag.Importance, TagType.Info);

		}
	}

	public enum TagType {
		Undefined,
		Info,
		Error
	}
}
