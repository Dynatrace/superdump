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
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyErrorFrameA", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "app.dll", "MyAppFrameA", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "app.dll", "MyAppFrameB", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrameA", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrameB", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MySystemFrameA", 123, 456, 789, 1011, 1213, null),
					})
			};
			AddTagToFrameAndThread(res1, SDTag.NativeExceptionTag);

			var res2 = new SDResult { ThreadInformation = new Dictionary<uint, SDThread>() };
			res2.ThreadInformation[1] = new SDThread(1) {
				StackTrace = new SDCombinedStackTrace(new List<SDCombinedStackFrame>() {
					new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyErrorFrameA", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "app.dll", "MyAppFrameZ", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "app.dll", "MyAppFrameA", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "app.dll", "MyAppFrameB", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrameA", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrameB", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrameC", 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MySystemFrameA", 123, 456, 789, 1011, 1213, null),
				})
			};
			AddTagToFrameAndThread(res2, SDTag.NativeExceptionTag);

			var similarity = CrashSimilarity.Calculate(CrashSimilarity.SDResultToMiniInfo(res1), CrashSimilarity.SDResultToMiniInfo(res2));

			Assert.Equal(0.875, similarity.StackTraceSimilarity);
			Assert.Equal(1.0, similarity.ModulesSimilarity);
			Assert.Null(similarity.LastEventSimilarity);
			Assert.Null(similarity.ExceptionSimilarity);
			Assert.Equal(0.9375, similarity.OverallSimilarity);
		}

		/// <summary>
		/// make sure similarity detection can handle null values for modules and methods
		/// </summary>
		[Fact]
		public void TestStacktraceSimilarityNullValues() {
			var res1 = new SDResult { ThreadInformation = new Dictionary<uint, SDThread>() };
			res1.ThreadInformation[1] = new SDThread(1) {
				StackTrace = new SDCombinedStackTrace(new List<SDCombinedStackFrame>() {
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyErrorFrameA", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, null, null, 123, 456, 789, 1011, 1213, null),
					})
			};
			AddTagToFrameAndThread(res1, SDTag.NativeExceptionTag);

			var res2 = new SDResult { ThreadInformation = new Dictionary<uint, SDThread>() };
			res2.ThreadInformation[1] = new SDThread(1) {
				StackTrace = new SDCombinedStackTrace(new List<SDCombinedStackFrame>() {
					new SDCombinedStackFrame(StackFrameType.Native, null, null, 123, 456, 789, 1011, 1213, null),
					new SDCombinedStackFrame(StackFrameType.Native, "app.dll", "MyAppFrameZ", 123, 456, 789, 1011, 1213, null),
				})
			};
			AddTagToFrameAndThread(res2, SDTag.NativeExceptionTag);

			var similarity = CrashSimilarity.Calculate(CrashSimilarity.SDResultToMiniInfo(res1), CrashSimilarity.SDResultToMiniInfo(res2));
			Assert.NotNull(similarity);
		}

		private static void AddTagToFrameAndThread(SDResult result, SDTag tag) {
			result.ThreadInformation[1].Tags.Add(tag);
			result.ThreadInformation[1].StackTrace[0].Tags.Add(tag);
		}

	}
}
