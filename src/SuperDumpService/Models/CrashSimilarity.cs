using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nest;
using SuperDump.Models;

namespace SuperDumpService.Models {
	public class CrashSimilarity {

		// this class is supposed to only consider similarities of features that identify the "problem", or "reason for the crash".
		// other features that might be similar between two dumps, but don't give information about the crash reason, should not be added here.

		// there is a number of dimensions, each dimension has it's own similarity value of 0.0-1.0
		private readonly IDictionary<string, double> similarities = new Dictionary<string, double>();

		private const string StacktraceKey = "STACKTRACE";
		private const string ModulesInStacktraceKey = "STACKTRACE_MODULES";
		private const string LastEventKey = "LASTEVENT";
		private const string ExceptionMessageKey = "EXCEPTION_MESSAGE";


		// there's an overall similartiy of 0.0-1.0, which is a weighted average of dimensions
		public double Value() => similarities.Values.Average();

		// in the UI, we could even show a table, where each dimension is shown separately (?)

		public static async Task<CrashSimilarity> Calculate(SDResult result, SDResult otherResult) {
			var similarity = new CrashSimilarity();

			similarity.similarities[StacktraceKey] = await CalculateStacktraceSimilarity(result, otherResult);
			similarity.similarities[ModulesInStacktraceKey] = await CalculateModulesInStacktraceSimilarity(result, otherResult);
			similarity.similarities[LastEventKey] = await CalculateLastEventSimilarity(result, otherResult);
			similarity.similarities[ExceptionMessageKey] = await CalculateExceptionMessageSimilarity(result, otherResult);

			return similarity;
		}

		private static async Task<double> CalculateStacktraceSimilarity(SDResult result, SDResult otherResult) {
			var t1 = result.GetErrorThread();
			var t2 = otherResult.GetErrorThread();

			if (t1 == null || t2 == null) return 0.0;

			// iterate over the top X stracktraces and compare each frame for similarity
			// CN -> not sure about this algorithm. there are cases where some frames differ while it's the same root cause. 
			// maybe just generally looking how many frames appear in the other stack might be ok.
			int take = 15;
			int count = 0;
			double methodsSimilarCount = 0;
			double modulesSimilarCount = 0;
			using (var iter1 = t1.StackTrace.GetEnumerator())
			using (var iter2 = t2.StackTrace.GetEnumerator()) {
				// iterate while both stacktraces still have elements
				while (take-- > 0 && iter1.MoveNext() && iter2.MoveNext()) {
					count++;
					methodsSimilarCount += iter1.Current.MethodName.Equals(iter2.Current.MethodName, StringComparison.OrdinalIgnoreCase)
						? 1.0
						: 0.0;
					modulesSimilarCount += iter1.Current.ModuleName.Equals(iter2.Current.ModuleName, StringComparison.OrdinalIgnoreCase)
						? 1.0
						: 0.0;
				}
			}

			return 0.0;
		}

		private static async Task<double> CalculateModulesInStacktraceSimilarity(SDResult result, SDResult otherResult) {
			return 0.0;
		}

		private static async Task<double> CalculateLastEventSimilarity(SDResult result, SDResult otherResult) {
			return 0.0;
		}

		private static async Task<double> CalculateExceptionMessageSimilarity(SDResult result, SDResult otherResult) {
			return 0.0;
		}
	}
}
