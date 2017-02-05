using System;

namespace SuperDump.Models {
	public class SDTag {
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
			var item = obj as SDTag;
			if (item == null) return false;
			return item.Name == this.Name;
		}

		public override int GetHashCode() {
			return this.Name.GetHashCode();
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
		public static readonly SDTag ClrWaitForGc = new SDTag("clr-wait-for-gc", 0);
		public static readonly SDTag ClrGcThread = new SDTag("clr-gc-thread", 0);
		public static readonly SDTag BreakInstructionTag = new SDTag("break-instruction", 0);

		public static bool ContainsAgentName(string thing) {
			string lower = thing.ToLower();
			return lower.Contains("oneagent")
				|| lower.Contains("dtagent")
				|| lower.Contains("ruxitagent");
		}
	}
}
