using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nest;
using SuperDump.Models;
using SuperDumpService.Helpers;

namespace SuperDumpService.Models {
	public class CrashSimilarity {

		// this class is supposed to only consider similarities of features that identify the "problem", or "reason for the crash".
		// other features that might be similar between two dumps, but don't give information about the crash reason, should not be added here.

		// indicates the version, MinInfos are built. if sematics in similarity detection changes, 
		// increment this version and mini-infos will be automatically re-created on the fly
		//   version 1: initial version
		//   version 2: removed ModuleName from stackframe comparison
		//   version 3: hashes instead of strings. breaking change. need to re-create all mini-infos.
		public const int MiniInfoVersion = 3;

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

		public static CrashSimilarity Calculate(in DumpMiniInfo result, in DumpMiniInfo otherResult) {
			var similarity = new CrashSimilarity();

			similarity.similarities[StacktraceKey] = CalculateStacktraceSimilarity(result, otherResult);
			similarity.similarities[ModulesInStacktraceKey] = CalculateModulesInStacktraceSimilarity(result, otherResult);
			similarity.similarities[LastEventKey] = CalculateLastEventSimilarity(result, otherResult);
			similarity.similarities[ExceptionMessageKey] = CalculateExceptionMessageSimilarity(result, otherResult);

			return similarity;
		}

		private static double? CalculateStacktraceSimilarity(in DumpMiniInfo resultA, in DumpMiniInfo resultB) {
			var errorThreadA = resultA.FaultingThread;
			var errorThreadB = resultB.FaultingThread;

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
			int AinBCount = CountAinB(errorThreadA.Value.DistinctFrameHashes, errorThreadB.Value.DistinctFrameHashes, (a, b) => a == b);
			int BinACount = CountAinB(errorThreadB.Value.DistinctFrameHashes, errorThreadA.Value.DistinctFrameHashes, (a, b) => a == b);

			double ainb = AinBCount / (double)errorThreadA.Value.DistinctFrameHashes.Length;
			double bina = BinACount / (double)errorThreadB.Value.DistinctFrameHashes.Length;

			return Math.Min(ainb, bina);
		}

		private static int CountAinB<T>(IEnumerable<T> a, IEnumerable<T> b, Func<T, T, bool> predicate) {
			int count = 0;
			foreach (var frame in a) {
				if (b.Any(x => predicate(x, frame))) count++;
			}
			return count;
		}

		/// <summary>
		/// Find the number of modules are the same in both stacks.
		///
		/// returns a number between 0.0 and 1.0, where 1.0 is full similarity, and 0.0 is no similarity
		/// </summary>
		private static double? CalculateModulesInStacktraceSimilarity(in DumpMiniInfo resultA, in DumpMiniInfo resultB) {
			var errorThreadA = resultA.FaultingThread;
			var errorThreadB = resultB.FaultingThread;

			if (errorThreadA == null && errorThreadB == null) return null; // no value in comparing if there are none
			if (errorThreadA == null ^ errorThreadB == null) return 0; // one result has an error thread, while the other one does not. inequal.

			var modulesA = errorThreadA.Value.DistinctModuleHashes;
			var modulesB = errorThreadB.Value.DistinctModuleHashes;

			int AinBCount = CountAinB(modulesA, modulesB, (a, b) => a == b);
			int BinACount = CountAinB(modulesB, modulesA, (a, b) => a == b);

			double ainb = AinBCount / (double)modulesA.Count();
			double bina = BinACount / (double)modulesB.Count();

			return Math.Min(ainb, bina);
		}

		private static double? CalculateLastEventSimilarity(in DumpMiniInfo resultA, in DumpMiniInfo resultB) {
			var lastEventA = resultA.LastEvent;
			var lastEventB = resultB.LastEvent;

			if (lastEventA == null && lastEventB == null) return null; // no value in comparing empty lastevent
			if (lastEventA == null ^ lastEventB == null) return 0; // one of the results has NO lastevent, while the other one does. let's define this as not-similar
			if (lastEventA.Value.TypeHash != lastEventB.Value.TypeHash) return 0.0;
			return lastEventA.Value.DescriptionHash == lastEventB.Value.DescriptionHash ? 1.0 : 0.0;
		}

		// relative similarity between two strings
		private static double StringSimilarity(string description1, string description2) {
			return Utility.LevenshteinSimilarity(Utility.StripNonAlphanumeric(description1), Utility.StripNonAlphanumeric(description2));
		}

		private static double? CalculateExceptionMessageSimilarity(in DumpMiniInfo resultA, in DumpMiniInfo resultB) {
			var exceptionA = resultA.Exception;
			var exceptionB = resultB.Exception;

			if (exceptionA == null && exceptionB == null) return null; // no value in comparing if there are none
			if (exceptionA == null ^ exceptionB == null) return 0; // one result has an error thread, while the other one does not. inequal.

			return (exceptionA.Value.TypeHash == exceptionB.Value.TypeHash
				&& exceptionA.Value.MessageHash == exceptionB.Value.MessageHash)
				? 1.0
				: 0.0;
			// omit comparison of StackTrace for now. maybe implement at later point if it makes sense.
		}

		public static DumpMiniInfo SDResultToMiniInfo(SDResult result) {
			var miniinfo = new DumpMiniInfo();

			// modules & stackframes
			var faultingThread = result.GetErrorOrLastExecutingThread();
			if (faultingThread != null) {
				miniinfo.FaultingThread = new ThreadMiniInfo() {
					DistinctModuleHashes = faultingThread.StackTrace.Select(x => Utility.StripNonAlphanumeric(x.ModuleName).GetStableHashCode()).Distinct().ToArray(),
					DistinctFrameHashes = faultingThread.StackTrace.Select(x => Utility.StripNonAlphanumeric(x.MethodName).GetStableHashCode()).Distinct().ToArray()
				};
			}

			// lastevent
			if (result.LastEvent != null) {
				if (result.LastEvent.Description.StartsWith("Break instruction exception")) {
					miniinfo.LastEvent = null; // "break instruction" as a lastevent is so generic, it's practically useless. treat it as if there was no information at all.
				} else { 
					miniinfo.LastEvent = new LastEventMiniInfo {
						TypeHash = result.LastEvent.Type?.GetStableHashCode(),
						DescriptionHash = result.LastEvent.Description.GetStableHashCode()
					};
				}
			}

			// exception
			var exception = result.GetErrorOrLastExecutingThread()?.LastException;
			if (exception != null) {
				miniinfo.Exception = new ExceptionMiniInfo() {
					TypeHash = exception.Type?.GetStableHashCode(),
					MessageHash = exception.Message?.GetStableHashCode()
				};
			}

			miniinfo.DumpSimilarityInfoVersion = MiniInfoVersion;

			return miniinfo;
		}
	}
}
