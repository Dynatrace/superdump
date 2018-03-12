using SuperDump.Analyzer.Common;
using SuperDump.Models;
using System;
using System.Linq;

namespace SuperDump.Analyzers {
	public class WindowsTagAnalyzer : DynamicAnalyzer {
		public override void AnalyzeResult(SDResult result) {
			if (result.LastEvent?.Description?.StartsWith("CLR exception") ?? false) {
				result.ThreadInformation.Values.Single(t => t.EngineId == result.LastEvent.ThreadId).Tags.Add(SDTag.ManagedExceptionTag);
			} else if (result.LastEvent?.Description?.StartsWith("Access violation") ?? false) {
				result.ThreadInformation.Values.Single(t => t.EngineId == result.LastEvent.ThreadId).Tags.Add(SDTag.NativeExceptionTag);
			} else if (result.LastEvent?.Description?.StartsWith("Break instruction exception") ?? false) {
				result.ThreadInformation.Values.Single(t => t.EngineId == result.LastEvent.ThreadId).Tags.Add(SDTag.BreakInstructionTag);
			} else if (result.LastEvent?.Description?.StartsWith("Stack overflow") ?? false) {
				result.ThreadInformation.Values.Single(t => t.EngineId == result.LastEvent.ThreadId).Tags.Add(SDTag.StackOverflowTag);
			}
		}

		public override void AnalyzeFrame(SDThread thread, SDCombinedStackFrame frame) {
			AddFrameAndThreadTagIf(thread, frame, () => frame.MethodName == "_purecall", SDTag.PureCallTag);
			AddFrameAndThreadTagIf(thread, frame, () => frame.MethodName == "abort", SDTag.AbortTag);
		}

		/// <summary>
		/// if <paramref name="func"/> returns true, set <paramref name="tag"/> on frame and thread
		/// </summary>
		private bool AddFrameAndThreadTagIf(SDThread thread, SDCombinedStackFrame frame, Func<bool> func, SDTag tag) {
			if (!func()) return false;

			frame.Tags.Add(tag);
			thread.Tags.Add(tag);
			return true;
		}
	}
}
