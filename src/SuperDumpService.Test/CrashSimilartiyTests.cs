using System;
using System.Collections.Generic;
using SuperDump.Models;
using SuperDumpService.Models;
using SuperDumpService.Services;
using Xunit;

namespace SuperDumpService.Test {
	public class CrashSimilartiyTests {
		[Fact]
		public void TestStacktraceSimilarity() {
			var res1 = new SDResult { ThreadInformation = new Dictionary<uint, SDThread>() };
			res1.ThreadInformation[1] = new SDThread(1) {
				StackTrace = new SDCombinedStackTrace(new List<SDCombinedStackFrame>() {
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyErrorFrame1", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "app.dll", "MyAppFrame1", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "app.dll", "MyAppFrame2", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrame1", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrame2", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrame3", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MySystemFrame1", 123, 456, 789, 1011, 1213, null),
					})
			};
			AddTagToFrameAndThread(res1, SDTag.NativeExceptionTag);

			var res2 = new SDResult { ThreadInformation = new Dictionary<uint, SDThread>() };
			res2.ThreadInformation[1] = new SDThread(1) {
				StackTrace = new SDCombinedStackTrace(new List<SDCombinedStackFrame>() {
					new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyErrorFrame1", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "app.dll", "MyAppFrame0", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "app.dll", "MyAppFrame1", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "app.dll", "MyAppFrame2", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrame1", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrame2", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrame3", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MySystemFrame1", 123, 456, 789, 1011, 1213, null),
				})
			};
			AddTagToFrameAndThread(res2, SDTag.NativeExceptionTag);

			var similarity = CrashSimilarity.Calculate(res1, res2);

			Assert.Equal(0.875, similarity.StackTraceSimilarity);
			Assert.Equal(1.0, similarity.ModulesSimilarity);
			Assert.Null(similarity.LastEventSimilarity);
			Assert.Null(similarity.ExceptionSimilarity);
			Assert.Equal(0.9375, similarity.OverallSimilarity);
		}

		private static void AddTagToFrameAndThread(SDResult result, SDTag tag) {
			result.ThreadInformation[1].Tags.Add(tag);
			result.ThreadInformation[1].StackTrace[0].Tags.Add(tag);
		}
	}
}
