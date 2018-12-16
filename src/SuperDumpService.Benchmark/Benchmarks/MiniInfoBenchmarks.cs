using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Options;
using SuperDump.Models;
using SuperDumpService.Benchmarks.Fakes;
using SuperDumpService.Helpers;
using SuperDumpService.Models;
using SuperDumpService.Services;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace SuperDumpService.Benchmark.Benchmarks {
	/// <summary>
	/// Test memory consumption of MiniInfo. It's important to keep it minimal.
	/// 
	/// before changes: 6.1kb
	/// </summary>
	[ShortRunJob]
	[MemoryDiagnoser]
	public class MiniInfoBenchmarks {
		private SDResult sdresult;
		private List<DumpMiniInfo> mininfos = new List<DumpMiniInfo>();

		public MiniInfoBenchmarks() { }

		[GlobalSetup]
		public void GlobalSetup() {
			sdresult = CreateSDResult();
		}

		private SDResult CreateSDResult() {
			var res = new SDResult { ThreadInformation = new Dictionary<uint, SDThread>() };
			res.ThreadInformation[1] = new SDThread(1) {
				StackTrace = new SDCombinedStackTrace(new List<SDCombinedStackFrame>() {
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyErrorFrameA", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "app.dll", "MyAppFrameA", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "app.dll", "MyAppFrameB", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrameA", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrameB", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MySystemFrameA", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdllasdfasdfasdf.dll", "MyFrameasdfasdfasdfasdfaswsdfgsdfgsdgorkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdllsdfghdfhdfgh.dll", "MyFramewasdfasdfasdfasdfasddgfsdforkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdlldfghdfghdfgh.dll", "MyFramewoasdfasdfasdfasdfasdfasdfsdfgdsfgsrkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdllasdfasdfasdf.dll", "MyFrameasdf2asdfasdfasdfas1wsdfgsdfgsdgorkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdllsdfghdfhdfgh.dll", "MyFramewasdfasdfasdfasdfas2ddgfsdforkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdlldf1ghdfghdfgh.dll", "MyFramewo2asdfasdfasdfasdfas3dfasdfsdfgdsfgsrkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdllasd2fasdfasdf.dll", "MyFrameasdfasdfasdfasdfaswsd3fgsdfgsdgorkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdllsdfg3hdfhdfgh.dll", "MyFramewa3sdfasdfasdfasdfasddg45fsdforkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdlldfghd455fghdfgh.dll", "MyFramewoasdfasdfasdfasdfasdfas6dfsdfgdsfgsrkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdllasdfa6sdfasdf.dll", "MyFram3easdfasdfasdfasdfaswsdfgsd7fgsdgorkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll5sdfghdfhdfgh.dll", "MyFram3ewasdfasdfasdfasdfasddgfsdf8orkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdlldf4ghdfghdfgh.dll", "MyFramewoasdfasdfasdfasdfasdfasdfsdfgdsfgsrkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdllasdfasdfasdf.dll", "MyFrame3asdfasdfasdfasdfasw3sdfgsdfg90sdgorkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdllsdf5ghdfhdfgh.dll", "MyFram3ewasdfasdfasdfasdfasddgfsdforkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdlldfg4hdfghdfgh.dll", "MyFramewoasdfasdfasdfasdfasdfasdfsdf08gdsfgsrkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdllasd3fasdfasdf.dll", "MyFra3measdfasdfa3sdfasdfasw3sdfgsdfgsdgo8rkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdllsdf2hdfhdfgh.dll", "MyFramewasdfasdfasdfasdfasddgfsdforkFra5meC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdlldfgh4fghdfgh.dll", "MyFramewoasdfasdfasdfasdfasdfasdfsdfgdsf66gsrkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdllasdfasdfasdf.dll", "MyFrameasdfasdfasdf3asdfaswsdfgsdfgsdgorkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdllsdf5ghdfhdfgh.dll", "MyFramewasdfasdfasdfasdfasd3dgfsdforkFrameC2", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdlldfghdfghdfgh.dll", "MyFr1amewoasdfa3sdfasdfasdfasdfasdfsdfgdsfgsrkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdllasd3f3asdfasdf.dll", "MyFra23measdfasdfasdfasdfaswsdfgsdfgsdgorkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdllsdfghdfhdfgh.dll", "MyFrame3wasdfasdfasdfasdfasddgfsdforkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdlldfgh4dfghdfgh.dll", "MyFramew4oasdfasd33fasdfa3sdfasdfasdfsdfgdsfgsrkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdlla3sdfasdfasdf.dll", "MyFrameas4dfasdfasd3fasdfaswsdfgsdfgsdgorkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdllsdfg4hdfhdfgh.dll", "MyFramewasdfasdfasdfa3sdfasddgfsdforkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdlldfghd3fghdfgh.dll", "MyFramewoa2sdfasdfas3dfa3sdfasdfasdfsdfgdsfgsrkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdllasd3f4asdfasdf.dll", "MyFrameasdf56asdfasdf3asdfaswsdfgsdfgsdgorkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdllsdfgh356fhdfgh.dll", "MyFramewasdf3asdfasd3fasdfasddgfsdforkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdlldfgh7dfg3hdfgh.dll", "MyFramewoasdfasd3fasdfasdfasdfasdfsdfgdsfgsrkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdllasdfa6s3dfasdf.dll", "MyFrameas3dfas2dfasdfasdfaswsdfgsdfgsdgorkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdl3lsdfg7hdfhdfgh.dll", "MyFramewas3d3fasdf3asdfasdfasddgfsdforkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdlldf3g7hdfghdfgh.dll", "MyFramewoasdfa4s3dfa33sdfaswsdfgsdfgsdgorkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdllsd7fgh3dfhdfgh.dll", "MyFramewasdfasdf4asdfa3sdfasddgfsdforkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdlldf7ghdfghd3fgh.dll", "MyFramewoasdfasdf3asdfas3dfasdfasdfsdfgdsfgsrkFrameC", 123, 456, 789, 1011, 1213, null),
					}),
				LastException = new SDClrException {
					Message = "Thread in invalid state.",
					Type = "System.Threading.ThreadStateException"
				}
			};
			AddTagToFrameAndThread(res, SDTag.NativeExceptionTag);

			res.LastEvent = new SDLastEvent {
				Type = "EXCEPTION",
				ThreadId = 133,
				Description = "Break instruction exception - code 80000003 (first/second chance not available)"
			};
			return res;
		}

		private static void AddTagToFrameAndThread(SDResult result, SDTag tag) {
			result.ThreadInformation[1].Tags.Add(tag);
			result.ThreadInformation[1].StackTrace[0].Tags.Add(tag);
		}

		[Benchmark]
		public void CreateMiniInfo() {
			// 128 bytes
			Console.WriteLine("hashed: " + MemUsageHashed());
			Console.WriteLine("hashed: " + MemUsageHashed());
			Console.WriteLine("hashed: " + MemUsageHashed());
			Console.WriteLine("hashed: " + MemUsageHashed());
			Console.WriteLine("hashed: " + MemUsageHashed());
			Console.WriteLine("hashed: " + MemUsageHashed());
			Console.WriteLine("hashed: " + MemUsageHashed());
			Console.WriteLine("hashed: " + MemUsageHashed());
			Console.WriteLine("hashed: " + MemUsageHashed());
			Console.WriteLine("hashed: " + MemUsageHashed());
			Console.WriteLine("hashed: " + MemUsageHashed());
			Console.WriteLine("hashed: " + MemUsageHashed());
			Console.WriteLine("hashed: " + MemUsageHashed());
			Console.WriteLine("hashed: " + MemUsageHashed());
			Console.WriteLine("hashed: " + MemUsageHashed());
			Console.WriteLine("hashed: " + MemUsageHashed());
		}

		private long MemUsageHashed() {
			var before = GC.GetTotalMemory(true);
			var x = CrashSimilarity.SDResultToMiniInfo(sdresult);
			var after = GC.GetTotalMemory(true);
			return after - before;
		}
	}
}
