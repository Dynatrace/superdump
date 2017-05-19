﻿using System;
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
using RazorLight;
using SuperDumpService.ViewModels;
using System.IO;

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
			var model = new SlackMessageViewModel();

			var res = dumpRepo.GetResult(dumpInfo.BundleId, dumpInfo.DumpId, out string error);

			var engine = EngineFactory.CreateEmbedded(typeof(SlackMessageViewModel));
			model.TopProperties.Add(dumpInfo.DumpType == DumpType.WindowsDump ? "Windows" : "Linux");
			model.DumpFilename = Path.GetFileName(dumpInfo.DumpFileName);
			model.Url = $"{superDumpUrl}/Home/Report?bundleId={dumpInfo.BundleId}&dumpId={dumpInfo.DumpId}";
			if (res != null) {
				model.TopProperties.Add(res.SystemContext.ProcessArchitecture);

				if (res.IsManagedProcess) model.TopProperties.Add(".NET");
				if (res.SystemContext.Modules.Any(x => x.FileName.Contains("jvm.dll"))) model.TopProperties.Add("Java");
				if (res.SystemContext.Modules.Any(x => x.FileName.Contains("jvm.so"))) model.TopProperties.Add("Java");
				if (res.SystemContext.Modules.Any(x => x.FileName.Contains("iiscore.dll"))) model.TopProperties.Add("IIS");
				if (res.SystemContext.Modules.Any(x => x.FileName.Contains("nginx.so"))) model.TopProperties.Add("NGINX");
				if (res.SystemContext.Modules.Any(x => x.FileName.Contains("httpd/modules"))) model.TopProperties.Add("Apache");
				if (res.SystemContext.Modules.Any(x => x.FileName.Contains("node.exe"))) model.TopProperties.Add("Node.js");

				var agentModules = res.SystemContext.Modules.Where(x => x.Tags.Any(t => t.Equals(SDTag.DynatraceAgentTag))).Select(m => m.ToString());
				model.AgentModules = agentModules.ToList();

				model.NumManagedExceptions = res.ThreadInformation.Count(x => x.Value.Tags.Any(t => t.Equals(SDTag.ManagedExceptionTag)));
				model.NumNativeExceptions = res.ThreadInformation.Count(x => x.Value.Tags.Any(t => t.Equals(SDTag.NativeExceptionTag)));
				model.NumAssertErrors = res.ThreadInformation.Count(x => x.Value.Tags.Any(t => t.Equals(SDTag.AssertionErrorTag)));

				SDThread managedExceptionThread = res.ThreadInformation.Values.FirstOrDefault(x => x.Tags.Any(t => t.Equals(SDTag.ManagedExceptionTag)));
				SDClrException clrException = managedExceptionThread?.LastException;
				if (clrException != null) {
					model.TopException = clrException.Type;
					model.Stacktrace = clrException.StackTrace.ToString();
				}

				if (res.LastEvent != null && !res.LastEvent.Description.Contains("Break instruction")) { // break instruction events are useless
					model.LastEvent = $"{res.LastEvent.Type}: {res.LastEvent.Description}";
				}
			}

			return engine.Parse("SlackMessage", model);
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
