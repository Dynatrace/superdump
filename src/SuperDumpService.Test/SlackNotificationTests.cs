using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SuperDump.Models;
using SuperDumpService.Benchmarks.Fakes;
using SuperDumpService.Helpers;
using SuperDumpService.Models;
using SuperDumpService.Services;
using Xunit;

namespace SuperDumpService.Test {

	public class SlackNotificationTests {

		[Fact]
		public async Task TestSlackNotification() {
			var settings = Options.Create(new SuperDumpSettings());
			var fakeDump = CreateFakeDump();
			var pathHelper = new PathHelper("", "", "");
			var dumpStorage = new FakeDumpStorage(new FakeDump[] { fakeDump });
			var dumpRepo = new DumpRepository(dumpStorage, pathHelper);
			var slackNotificationService = new SlackNotificationService(settings, dumpRepo);
			var msg = await slackNotificationService.GetMessageModel(fakeDump.MetaInfo);

			// these assertions are bare mininum for now. Could be improved.
			Assert.Equal(1, msg.NumNativeExceptions);

			var msgStr = await slackNotificationService.GetMessage(fakeDump.MetaInfo);
			Assert.NotNull(msgStr);
		}

		private FakeDump CreateFakeDump() {
			var res = new SDResult { ThreadInformation = new Dictionary<uint, SDThread>() };
			res.ThreadInformation[1] = new SDThread(1) {
				StackTrace = new SDCombinedStackTrace(new List<SDCombinedStackFrame>() {
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyErrorFrameA", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "app.dll", "MyAppFrameA", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "app.dll", "MyAppFrameB", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrameA", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrameB", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MySystemFrameA", 123, 456, 789, 1011, 1213, null)
					})
			};
			AddTagToFrameAndThread(res, SDTag.NativeExceptionTag);

			res.SystemContext = new SDSystemContext {
				ProcessArchitecture = "X64",
				Modules = new List<SDModule> { new SDModule { FileName = "jvm.dll" } }
			};

			return new FakeDump {
				MetaInfo = new DumpMetainfo { BundleId = $"bundle1", DumpId = $"dump1", Status = DumpStatus.Finished },
				FileInfo = null,
				Result = res,
				MiniInfo = CrashSimilarity.SDResultToMiniInfo(res)
			};
		}

		private static void AddTagToFrameAndThread(SDResult result, SDTag tag) {
			result.ThreadInformation[1].Tags.Add(tag);
			result.ThreadInformation[1].StackTrace[0].Tags.Add(tag);
		}
	}
}
