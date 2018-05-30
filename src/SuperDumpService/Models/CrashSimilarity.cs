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
		private readonly IDictionary<string, double?> similarities = new Dictionary<string, double?>();

		private const string StacktraceKey = "STACKTRACE";
		private const string ModulesInStacktraceKey = "STACKTRACE_MODULES";
		private const string LastEventKey = "LASTEVENT";
		private const string ExceptionMessageKey = "EXCEPTION_MESSAGE";

		public double? StackTraceSimilarity => similarities[StacktraceKey];
		public double? ModulesSimilarity => similarities[ModulesInStacktraceKey];
		public double? LastEventSimilarity => similarities[LastEventKey];
		public double? ExceptionSimilarity => similarities[ExceptionMessageKey];

		// there's an overall similartiy of 0.0-1.0, which is a weighted average of dimensions
		public double OverallSimilarity => ValidSimilarities.Any() ? ValidSimilarities.Average() : 0;
		private IEnumerable<double> ValidSimilarities => similarities.Values.Where(x => x.HasValue).Select(x => x.Value);

		// in the UI, we could even show a table, where each dimension is shown separately (?)

		public static CrashSimilarity Calculate(SDResult result, SDResult otherResult) {
			var similarity = new CrashSimilarity();

			similarity.similarities[StacktraceKey] = CalculateStacktraceSimilarity(result, otherResult);
			similarity.similarities[ModulesInStacktraceKey] = CalculateModulesInStacktraceSimilarity(result, otherResult);
			similarity.similarities[LastEventKey] = CalculateLastEventSimilarity(result, otherResult);
			similarity.similarities[ExceptionMessageKey] = CalculateExceptionMessageSimilarity(result, otherResult);

			return similarity;
		}

		private static double? CalculateStacktraceSimilarity(SDResult resultA, SDResult resultB) {
			var errorThreadA = resultA.GetErrorOrLastExecutingThread();
			var errorThreadB = resultB.GetErrorOrLastExecutingThread();

			if (errorThreadA == null && errorThreadB == null) return null; // no value in comparing if there are none
			if (errorThreadA == null ^ errorThreadB == null) return 0; // one result has an error thread, while the other one does not. inequal.

			// approach 1: iterate over the top X stracktraces and compare each frame for similarity
			// CN -> not sure about this algorithm. there are cases where some frames differ while it's the same root cause. 
			// maybe just generally looking how many frames appear in the other stack might be ok.
			//int take = 15;
			//int count = 0;
			//double methodsSimilarCount = 0;
			//double modulesSimilarCount = 0;
			//using (var iter1 = t1.StackTrace.GetEnumerator())
			//using (var iter2 = t2.StackTrace.GetEnumerator()) {
			//	// iterate while both stacktraces still have elements
			//	while (take-- > 0 && iter1.MoveNext() && iter2.MoveNext()) {
			//		count++;
			//		methodsSimilarCount += iter1.Current.MethodName.Equals(iter2.Current.MethodName, StringComparison.OrdinalIgnoreCase)
			//			? 1.0
			//			: 0.0;
			//		modulesSimilarCount += iter1.Current.ModuleName.Equals(iter2.Current.ModuleName, StringComparison.OrdinalIgnoreCase)
			//			? 1.0
			//			: 0.0;
			//	}
			//}

			// approach 2: apply "weight" to each stackframe. it should represent the relevance of the frame to the crash-reason.
			// treat all "error"-frames as 1.0. adjacent frames get linearly decreasing weight.
			// TBD


			// approach 3: let's start with a dead-simple alg. look how many frames of stack A appear in stack B and vice versa.
			int AinBCount = CountAinB(errorThreadA.StackTrace, errorThreadB.StackTrace, FrameEquals);
			int BinACount = CountAinB(errorThreadB.StackTrace, errorThreadA.StackTrace, FrameEquals);

			double ainb = AinBCount / (double)errorThreadA.StackTrace.Count;
			double bina = BinACount / (double)errorThreadB.StackTrace.Count;

			return Math.Min(ainb, bina);
		}

		private static int CountAinB<T>(IEnumerable<T> a, IEnumerable<T> b, Func<T, T, bool> predicate) {
			int count = 0;
			foreach (var frame in a) {
				if (b.Any(x => predicate(x, frame))) count++;
			}
			return count;
		}

		private static bool FrameEquals(SDCombinedStackFrame frameA, SDCombinedStackFrame frameB) {
			return EqualsIgnoreNonAscii(frameA.ModuleName, frameB.ModuleName)
				&& EqualsIgnoreNonAscii(frameA.MethodName, frameB.MethodName);
		}

		/// <summary>
		/// Find the number of modules are the same in both stacks.
		///
		/// returns a number between 0.0 and 1.0, where 1.0 is full similarity, and 0.0 is no similarity
		/// </summary>
		private static double? CalculateModulesInStacktraceSimilarity(SDResult resultA, SDResult resultB) {
			var errorThreadA = resultA.GetErrorOrLastExecutingThread();
			var errorThreadB = resultB.GetErrorOrLastExecutingThread();

			if (errorThreadA == null && errorThreadB == null) return null; // no value in comparing if there are none
			if (errorThreadA == null ^ errorThreadB == null) return 0; // one result has an error thread, while the other one does not. inequal.

			var modulesA = errorThreadA.StackTrace.Select(x => x.ModuleName).Distinct();
			var modulesB = errorThreadB.StackTrace.Select(x => x.ModuleName).Distinct();

			int AinBCount = CountAinB(modulesA, modulesB, (a, b) => EqualsIgnoreNonAscii(a, b));
			int BinACount = CountAinB(modulesB, modulesA, (a, b) => EqualsIgnoreNonAscii(a, b));

			double ainb = AinBCount / (double)modulesA.Count();
			double bina = BinACount / (double)modulesB.Count();

			return Math.Min(ainb, bina);
		}

		private static double? CalculateLastEventSimilarity(SDResult resultA, SDResult resultB) {
			var lastEventA = resultA.LastEvent;
			var lastEventB = resultB.LastEvent;

			// "break instruction" as a lastevent is so generic, it's practically useless. treat it as if there was no information at all.
			if (lastEventA != null && lastEventA.Description.StartsWith("Break instruction exception")) lastEventA = null;
			if (lastEventB != null && lastEventB.Description.StartsWith("Break instruction exception")) lastEventB = null;

			if (lastEventA == null && lastEventB == null) return null; // no value in comparing empty lastevent
			if (lastEventA == null ^ lastEventB == null) return 0; // one of the results has NO lastevent, while the other one does. let's define this as not-similar
			return EqualsIgnoreNonAscii(lastEventA.Type, lastEventB.Type)
				&& EqualsIgnoreNonAscii(lastEventA.Description, lastEventB.Description)
					? 1.0
					: 0.0;
		}

		private static double? CalculateExceptionMessageSimilarity(SDResult resultA, SDResult resultB) {
			var exceptionA = resultA.GetErrorOrLastExecutingThread()?.LastException;
			var exceptionB = resultB.GetErrorOrLastExecutingThread()?.LastException;

			if (exceptionA == null && exceptionB == null) return null; // no value in comparing if there are none
			if (exceptionA == null ^ exceptionB == null) return 0; // one result has an error thread, while the other one does not. inequal.

			return EqualsIgnoreNonAscii(exceptionA.Type, exceptionB.Type)
				&& EqualsIgnoreNonAscii(exceptionA.Message, exceptionB.Message)
				? 1.0
				: 0.0;
			// omit comparison of StackTrace for now. maybe implement at later point if it makes sense.
		}

		/// <summary>
		/// frames sometimes contain addresses. let's just strip out all non-ascii characters. comparison should still be valid enough.
		/// even modules contain addresses in some cases. also lastevent or exception text.
		/// </summary>
		private static bool EqualsIgnoreNonAscii(string s1, string s2) {
			if (s1 == null || s2 == null) return false;
			return StripNonAscii(s1).Equals(StripNonAscii(s2), StringComparison.OrdinalIgnoreCase);
		}

		private static string StripNonAscii(string str) {
			return new string(str.ToLower().Where(c => c >= 'a' && c <= 'z').ToArray());
		}
	}
}
