using SuperDump.Models;

namespace SuperDump.Analyzer.Common
{
	public class UniversalTagAnalyzer : DynamicAnalyzer {
		public override void AnalyzeResult(SDResult result) {
			if (result.ThreadInformation.ContainsKey(result.LastExecutedThread)) {
				result.ThreadInformation[result.LastExecutedThread].Tags.Add(SDTag.LastExecutingTag);
			}
		}
	}
}
