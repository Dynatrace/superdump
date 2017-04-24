using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SuperDumpService.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;
using System.Net;
using Slack.Webhooks.Core;
using SuperDump.Models;

namespace SuperDumpService.Services {
	public class SlackNotificationService {
		private readonly string superDumpUrl;
		private readonly string[] webhookUrls;
		private readonly DumpRepository dumpRepo;

		public SlackNotificationService(IOptions<SuperDumpSettings> settings, DumpRepository dumpRepo) {
			this.superDumpUrl = settings.Value.SuperDumpUrl;
			this.webhookUrls = settings.Value.SlackNotificationUrls;
			this.dumpRepo = dumpRepo;
		}

		public async Task NotifyDumpAnalysisFinished(DumpMetainfo dumpInfo) {
			if (this.webhookUrls == null) return;

			try {
				string msg = GetMessage(dumpInfo);

				foreach (string webhook in webhookUrls) {
					await SendMessage(webhook, msg);
				}
			} catch (Exception e) {
				Console.WriteLine($"Slack notifications failed: {e}");
			}
		}

		public string GetMessage(DumpMetainfo dumpInfo) {
			var dump = dumpRepo.GetResult(dumpInfo.BundleId, dumpInfo.DumpId, out string error);

			string winlinux = dumpInfo.DumpType == DumpType.WindowsDump ? "Windows" : "Linux";
			string dumpfilename = dumpInfo.DumpFileName;
			string url = string.IsNullOrEmpty(superDumpUrl) ? "`bundleId={dumpInfo.BundleId}, dumpId={dumpInfo.DumpId}`" : $"<{superDumpUrl}/Home/Report?bundleId={dumpInfo.BundleId}&dumpId={dumpInfo.DumpId}>";
			string arch = dump == null ? "" : $", {dump.SystemContext.ProcessArchitecture}";
			string processType = string.Empty;
			string agentModuleStr = string.Empty;
			string errors = string.Empty;

			if (dump != null) {
				if (dump.IsManagedProcess) processType = ", .NET";
				if (dump.SystemContext.Modules.Any(x => x.FileName.Contains("jvm.dll"))) processType = ", Java";

				var agentModules = dump.SystemContext.Modules.Where(x => x.Tags.Any(t => t.Equals(SDTag.DynatraceAgentTag))).Select(m => m.ToString()).ToArray();
				agentModuleStr = string.Join(", ", agentModules);

				int numManagedExceptions = dump.ThreadInformation.Count(x => x.Value.Tags.Any(t => t.Equals(SDTag.ManagedExceptionTag)));
				int numNativeExceptions = dump.ThreadInformation.Count(x => x.Value.Tags.Any(t => t.Equals(SDTag.NativeExceptionTag)));
				int numAssertErrors = dump.ThreadInformation.Count(x => x.Value.Tags.Any(t => t.Equals(SDTag.AssertionErrorTag)));
				if (numManagedExceptions > 0) errors += $"\n{numManagedExceptions} thread(s) with .NET exceptions";
				if (numNativeExceptions > 0) errors += $"\n{numManagedExceptions} thread(s) with native exceptions";
				if (numAssertErrors > 0) errors += $"\n{numManagedExceptions} thread(s) with assertion errors";
			}

			return $"*New dump analyzed (_{winlinux}{arch}{processType}_)*\n```{dumpfilename}\nagents: {agentModuleStr}{errors}```\n{url}";
		}

		private async Task SendMessage(string url, string msg) {
			var client = new SlackClient(url);
			await client.PostAsync(new SlackMessage() {
				Username = "SuperDump",
				IconEmoji = ":ghost:",
				Text = msg
			});
		}
	}
}
