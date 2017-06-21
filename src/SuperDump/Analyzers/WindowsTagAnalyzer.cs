using SuperDump.Analyzer.Common;
using SuperDump.Models;
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
			}
		}
	}
}
