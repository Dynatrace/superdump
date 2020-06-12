using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SuperDump.Models;
using SuperDumpService.Models;
using SuperDumpService.Services;
using Xunit;

namespace SuperDumpService.Test {
	public class FaultReportingTests {

		// .net managed exception, from bundleId=hqe1548&dumpId=mxc5990
		[Fact]
		public void TestFaultReporting1() {
			var res1 = new SDResult { ThreadInformation = new Dictionary<uint, SDThread>() };
			res1.ThreadInformation[1] = new SDThread(61) {
				StackTrace = new SDCombinedStackTrace(new List<SDCombinedStackFrame>() {
					new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyErrorFrameA", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "app.dll", "MyAppFrameA", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "app.dll", "MyAppFrameB", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrameA", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrameB", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrameC", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MySystemFrameA", 123, 456, 789, 1011, 1213, null),
				}),
				Index = 61,
				EngineId = 61,
				OsId = 18752,
				LastException = new SDClrException() {
					OSThreadId = 18752,
					Type = null,
					Message = "Could not load file or assembly 'Microsoft.Diagnostics.Tracing.EventSource, Version=1.1.28.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' or one of its dependencies. The system cannot find the file specified."
				}
			};
			AddTagToFrameAndThread(res1, SDTag.NativeExceptionTag);

			res1.LastEvent = new SDLastEvent("EXCEPTION", "CLR exception - code e0434352 (first/second chance not available)", 61);
			var report = FaultReportCreator.CreateFaultReport(res1);

			// test if expected pieces of data exist in report
			Assert.NotNull(report.FaultReason);
			Assert.Contains("EXCEPTION", report.FaultReason);
			Assert.Contains("CLR exception - code e0434352", report.FaultReason);
			Assert.Contains("Could not load file or assembly", report.FaultReason);

			Assert.NotNull(report.FaultingFrames);
			Assert.Equal(7, report.FaultingFrames.Count);
		}

		// native access violation, from bundleId=ygz3901&dumpId=maf2473
		[Fact]
		public void TestFaultReporting2() {
			var res1 = new SDResult { ThreadInformation = new Dictionary<uint, SDThread>() };
			res1.ThreadInformation[1] = new SDThread(39636) {
				StackTrace = new SDCombinedStackTrace(new List<SDCombinedStackFrame>() {
					new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyErrorFrameA", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "app.dll", "MyAppFrameA", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "app.dll", "MyAppFrameB", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrameA", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrameB", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrameC", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MySystemFrameA", 123, 456, 789, 1011, 1213, null),
				}),
				Index = 0,
				EngineId = 0,
				OsId = 39636
			};
			AddTagToFrameAndThread(res1, SDTag.NativeExceptionTag);

			res1.LastEvent = new SDLastEvent("EXCEPTION", "Access violation - code c0000005 (first/second chance not available)", 0);
			var report = FaultReportCreator.CreateFaultReport(res1);

			// test if expected pieces of data exist in report
			Assert.NotNull(report.FaultReason);
			Assert.Contains("EXCEPTION", report.FaultReason);
			Assert.Contains("Access violation - code c0000005 (first/second chance not available)", report.FaultReason);

			Assert.NotNull(report.FaultingFrames);
			Assert.Equal(7, report.FaultingFrames.Count);
		}

		// linux abort, from bundleId=fni4863&dumpId=ddm1365
		[Fact]
		public void TestFaultReporting3() {
			var res1 = new SDResult { ThreadInformation = new Dictionary<uint, SDThread>() };
			res1.ThreadInformation[1] = new SDThread(0) {
				StackTrace = new SDCombinedStackTrace(new List<SDCombinedStackFrame>() {
					new SDCombinedStackFrame(StackFrameType.Native, "libc-2.17.so", "MyErrorFrameA", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "libc-2.17.so", "MyAppFrameA", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "libc-2.17.so", "MyAppFrameB", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "libtux.so", "MyFrameworkFrameA", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "libtux.so", "MyFrameworkFrameB", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "ntdll.so", "MyFrameworkFrameC", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "srvsnl_mqha.so", "MySystemFrameA", 123, 456, 789, 1011, 1213, null),
				}),
				Index = 0,
				EngineId = 0,
				OsId = 0
			};
			
			AddTagToFrameAndThread(res1, SDTag.NativeExceptionTag);

			res1.LastEvent = new SDLastEvent {
				Type = "6",
				Description = "SIGABRT",
				ThreadId = 0
			};
			var report = FaultReportCreator.CreateFaultReport(res1);

			// test if expected pieces of data exist in report
			Assert.NotNull(report.FaultReason);
			Assert.Contains("SIGABRT", report.FaultReason);

			Assert.NotNull(report.FaultingFrames);
			Assert.Equal(7, report.FaultingFrames.Count);
		}

		[Fact]
		public void TestFaultReportingLongStacktrace() {
			var res1 = new SDResult { ThreadInformation = new Dictionary<uint, SDThread>() };

			// Thread with 1000 stack frames
			res1.ThreadInformation[1] = new SDThread(0) {
				StackTrace = new SDCombinedStackTrace(
					Enumerable.Repeat(new SDCombinedStackFrame(StackFrameType.Native, "lib.so", "MyErrorFrameA", 123, 456, 789, 1011, 1213, null), 1000).ToList()
				),
				Index = 0,
				EngineId = 0,
				OsId = 0
			};

			AddTagToFrameAndThread(res1, SDTag.NativeExceptionTag);

			res1.LastEvent = new SDLastEvent {
				Type = "6",
				Description = "SIGABRT",
				ThreadId = 0
			};
			var report = FaultReportCreator.CreateFaultReport(res1, 50); // maxFrames = 50

			// test if long stacktrace is properly cut
			Assert.NotNull(report.FaultingFrames);
			Assert.Equal(50+1, report.FaultingFrames.Count); // only 50 frames allowed, plus 1 for "..."
		}

		private static void AddTagToFrameAndThread(SDResult result, SDTag tag) {
			result.ThreadInformation[1].Tags.Add(tag);
			result.ThreadInformation[1].StackTrace[0].Tags.Add(tag);
		}

		private class TestFaultReportSender : IFaultReportSender {
			public List<FaultReport> Reports { get; private set; } = new List<FaultReport>();
			
			public Task SendFaultReport(DumpMetainfo dumpInfo, FaultReport faultReport) {
				Reports.Add(faultReport);
				return Task.CompletedTask;
			}
		}
	}
}
