using System;
using BenchmarkDotNet.Attributes;
using SuperDumpService.Models;

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
					Message = "Thread in invalid state.",
					Type = "System.Threading.ThreadStateException"
				},
				FaultingThread = new ThreadMiniInfo {
					DistinctModules = new string[] { "ntdll", "kernelbase", "ptsrv", "msvcr", "kernel" },
					DistrinctFrames = new string[] { "ntalpcsendwaitreceiveport", "sendmessagetowerservice", "reportexceptioninternal", "rtlreportexceptionhelper", "rtlreportexception", "ldrpcalloutexceptionfilter", "ldrpprocessdetachnode", "ldrpunloadnode", "ldrpdecrementmoduleloadcountex", "ldrunloaddll", "freelibrary", "hookfreelibrary", "unknown", "exit", "basethreadinitthunk", "rtluserthreadstart" }
				},
				LastEvent = new SuperDump.Models.SDLastEvent {
					Type = "EXCEPTION",
					ThreadId = 177,
					Description = "Break instruction exception - code 80000003 (first/second chance not available)"
				}
			};

			dump2 = new DumpMiniInfo {
				DumpSimilarityInfoVersion = 2,
				Exception = new ExceptionMiniInfo {
					Message = "Thread in invalid state.",
					Type = "System.Threading.ThreadStateException"
				},
				FaultingThread = new ThreadMiniInfo {
					DistinctModules = new string[] { "someotherlibrary", "ntdll", "kernelbase", "ptsrv", "msvcr", "kernel" },
					DistrinctFrames = new string[] { "someotherstackframe", "yetanotherunknown", "reportexceptioninternal", "rtlreportexceptionhelper", "rtlreportexception", "ldrpcalloutexceptionfilter", "ldrpprocessdetachnode", "ldrpunloadnode", "ldrpdecrementmoduleloadcountex", "ldrunloaddll", "freelibrary", "hookfreelibrary", "unknown", "exit", "basethreadinitthunk", "rtluserthreadstart" }
				},
				LastEvent = new SuperDump.Models.SDLastEvent {
					Type = "EXCEPTION",
					ThreadId = 133,
					Description = "Break instruction exception - code 80000003 (first/second chance not available)"
				}
			};

			dump3 = new DumpMiniInfo {
				DumpSimilarityInfoVersion = 2,
				Exception = null,
				FaultingThread = new ThreadMiniInfo {
					DistinctModules = new string[] { "clr", "mscorlib", "simpleinjector", "unknown", "lineosweb", "", "systemweb", "webengine", "iiscore", "kernel", "ntdll" },
					DistrinctFrames = new string[] { "sigtypecontextequal", "eehashtablebasesigtypecontextconsteeinstantiationhashtablehelperfinditem", "eehashtablebasesigtypecontextconsteeinstantiationhashtablehelpergetvalue", "methodcallgraphpreparerrun", "preparemethoddesc", "reflectioninvocationpreparedelegatehelper", "reflectioninvocationpreparedelegate", "mscorlibdllunknown", "simpleinjectordllunknown", "unknown", "lineoswebdllunknown", "calldescrworkerinternal", "calldescrworkerwithhandler", "calldescrworkerreflectionwrapper", "runtimemethodhandleinvokemethod", "debuggerumcatchhandlerframe", "systemwebdllunknown", "inlinedcallframe", "wmgdhandlerprocessnotification", "wmgdhandlerdowork", "requestdowork", "cmgdenghttpmoduleonacquirerequeststate", "notificationcontextrequestdowork", "notificationcontextcallmodulesinternal", "notificationcontextcallmodules", "notificationmaindowork", "wcontextbasecontinuenotificationloop", "wcontextbaseindicatecompletion", "wmgdhandlerindicatecompletion", "mgdindicatecompletion", "domainneutralilstubclassilstubpinvoke", "ummthunkwrapper", "threaddoadcallback", "contexttransitionframe", "ummdoadcallback", "processnotificationcallback", "unmanagedperappdomaintpcountdispatchworkitem", "threadpoolmgrexecuteworkrequest", "threadpoolmgrworkerthreadstart", "threadintermediatethreadproc", "basethreadinitthunk", "rtluserthreadstart" }
				},
				LastEvent = new SuperDump.Models.SDLastEvent {
					Type = "EXCEPTION",
					ThreadId = 55,
					Description = "Access violation - code c0000005 (first/second chance not available)"
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
