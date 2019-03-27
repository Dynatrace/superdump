using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using SuperDumpService.Models;
using SuperDumpService.Helpers;

namespace SuperDumpService.Benchmark.Benchmarks {

	[ShortRunJob]
	public class SimilarityCalculationBenchmarks {
		private readonly DumpMiniInfo dump1;
		private readonly DumpMiniInfo dump2; // very similar to dump1
		private readonly DumpMiniInfo dump3; // very different to dump1

		public SimilarityCalculationBenchmarks() {
			dump1 = new DumpMiniInfo {
				DumpSimilarityInfoVersion = 2,
				Exception = new ExceptionMiniInfo {
					MessageHash = "Thread in invalid state.".GetStableHashCode(),
					TypeHash = "System.Threading.ThreadStateException".GetStableHashCode()
				},
				FaultingThread = new ThreadMiniInfo {
					DistinctModuleHashes = new string[] { "ntdll", "kernelbase", "ptsrv", "msvcr", "kernel" }.Select(x => x.GetStableHashCode()).ToArray(),
					DistinctFrameHashes = new string[] { "ntalpcsendwaitreceiveport", "sendmessagetowerservice", "reportexceptioninternal", "rtlreportexceptionhelper", "rtlreportexception", "ldrpcalloutexceptionfilter", "ldrpprocessdetachnode", "ldrpunloadnode", "ldrpdecrementmoduleloadcountex", "ldrunloaddll", "freelibrary", "hookfreelibrary", "unknown", "exit", "basethreadinitthunk", "rtluserthreadstart" }.Select(x => x.GetStableHashCode()).ToArray()
				},
				LastEvent = new LastEventMiniInfo {
					TypeHash = "EXCEPTION".GetStableHashCode(),
					DescriptionHash = "Break instruction exception - code 80000003 (first/second chance not available)".GetStableHashCode()
				}
			};

			dump2 = new DumpMiniInfo {
				DumpSimilarityInfoVersion = 2,
				Exception = new ExceptionMiniInfo {
					MessageHash = "Thread in invalid state.".GetStableHashCode(),
					TypeHash = "System.Threading.ThreadStateException".GetStableHashCode()
				},
				FaultingThread = new ThreadMiniInfo {
					DistinctModuleHashes = new string[] { "someotherlibrary", "ntdll", "kernelbase", "ptsrv", "msvcr", "kernel" }.Select(x => x.GetStableHashCode()).ToArray(),
					DistinctFrameHashes = new string[] { "someotherstackframe", "yetanotherunknown", "reportexceptioninternal", "rtlreportexceptionhelper", "rtlreportexception", "ldrpcalloutexceptionfilter", "ldrpprocessdetachnode", "ldrpunloadnode", "ldrpdecrementmoduleloadcountex", "ldrunloaddll", "freelibrary", "hookfreelibrary", "unknown", "exit", "basethreadinitthunk", "rtluserthreadstart" }.Select(x => x.GetStableHashCode()).ToArray()
				},
				LastEvent = new LastEventMiniInfo {
					TypeHash = "EXCEPTION".GetStableHashCode(),
					DescriptionHash = "Break instruction exception - code 80000003 (first/second chance not available)".GetStableHashCode()
				}
			};

			dump3 = new DumpMiniInfo {
				DumpSimilarityInfoVersion = 2,
				Exception = null,
				FaultingThread = new ThreadMiniInfo {
					DistinctModuleHashes = new string[] { "clr", "mscorlib", "simpleinjector", "unknown", "lineosweb", "", "systemweb", "webengine", "iiscore", "kernel", "ntdll" }.Select(x => x.GetStableHashCode()).ToArray(),
					DistinctFrameHashes = new string[] { "sigtypecontextequal", "eehashtablebasesigtypecontextconsteeinstantiationhashtablehelperfinditem", "eehashtablebasesigtypecontextconsteeinstantiationhashtablehelpergetvalue", "methodcallgraphpreparerrun", "preparemethoddesc", "reflectioninvocationpreparedelegatehelper", "reflectioninvocationpreparedelegate", "mscorlibdllunknown", "simpleinjectordllunknown", "unknown", "lineoswebdllunknown", "calldescrworkerinternal", "calldescrworkerwithhandler", "calldescrworkerreflectionwrapper", "runtimemethodhandleinvokemethod", "debuggerumcatchhandlerframe", "systemwebdllunknown", "inlinedcallframe", "wmgdhandlerprocessnotification", "wmgdhandlerdowork", "requestdowork", "cmgdenghttpmoduleonacquirerequeststate", "notificationcontextrequestdowork", "notificationcontextcallmodulesinternal", "notificationcontextcallmodules", "notificationmaindowork", "wcontextbasecontinuenotificationloop", "wcontextbaseindicatecompletion", "wmgdhandlerindicatecompletion", "mgdindicatecompletion", "domainneutralilstubclassilstubpinvoke", "ummthunkwrapper", "threaddoadcallback", "contexttransitionframe", "ummdoadcallback", "processnotificationcallback", "unmanagedperappdomaintpcountdispatchworkitem", "threadpoolmgrexecuteworkrequest", "threadpoolmgrworkerthreadstart", "threadintermediatethreadproc", "basethreadinitthunk", "rtluserthreadstart" }.Select(x => x.GetStableHashCode()).ToArray()
				},
				LastEvent = new LastEventMiniInfo {
					TypeHash = "EXCEPTION".GetStableHashCode(),
					DescriptionHash = "Access violation - code c0000005 (first/second chance not available)".GetStableHashCode()
				}
			};
		}

		[Benchmark]
		public void CalculateSimilaritySamedump() {
			CrashSimilarity.Calculate(dump1, dump1);
		}

		[Benchmark]
		public void CalculateSimilaritySimilardumps() {
			CrashSimilarity.Calculate(dump1, dump2);
		}

		[Benchmark]
		public void CalculateSimilarityDifferentdumps() {
			CrashSimilarity.Calculate(dump1, dump3);
		}
	}
}
