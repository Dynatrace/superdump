using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SuperDump.Common;
using SuperDumpService.Helpers;
using SuperDumpService.Models;
using System.Diagnostics;
using SuperDump.Models;
using Dynatrace.OneAgent.Sdk.Api;
using Dynatrace.OneAgent.Sdk.Api.Enums;
using Dynatrace.OneAgent.Sdk.Api.Infos;
using SuperDumpService.Services.Analyzers;
using System.Collections.Generic;

namespace SuperDumpService.Services {
	public class AnalysisService {
		private readonly DumpRepository dumpRepo;
		private readonly BundleRepository bundleRepo;
		private readonly PathHelper pathHelper;
		private readonly NotificationService notifications;
		private readonly AnalyzerPipeline analyzerPipeline;
		private readonly IOneAgentSdk dynatraceSdk;
		private readonly IMessagingSystemInfo messagingSystemInfo;

		public AnalysisService(
					DumpRepository dumpRepo,
					BundleRepository bundleRepo,
					PathHelper pathHelper,
					NotificationService notifications,
					AnalyzerPipeline analyzerPipeline, 
					IOneAgentSdk dynatraceSdk
				) {

			this.dumpRepo = dumpRepo;
			this.bundleRepo = bundleRepo;
			this.pathHelper = pathHelper;
			this.notifications = notifications;
			this.analyzerPipeline = analyzerPipeline;
			this.dynatraceSdk = dynatraceSdk;
			messagingSystemInfo = dynatraceSdk.CreateMessagingSystemInfo("Hangfire", "analysis", MessageDestinationType.QUEUE, ChannelType.IN_PROCESS, null);
		}

		public async Task<IEnumerable<DumpMetainfo>> InitialAnalysis(string bundleId, DirectoryInfo directory) {
			foreach (InitalAnalyzerJob initalAnalyzerJob in analyzerPipeline.InitialAnalyzers) {
				IEnumerable<DumpMetainfo> dumpMetainfos = await initalAnalyzerJob.CreateDumpInfos(bundleId, directory);
				if (dumpMetainfos != null && dumpMetainfos.Any()) {
					return dumpMetainfos;
				}
			}
			return Enumerable.Empty<DumpMetainfo>();
		}

		public void ScheduleDumpAnalysis(IEnumerable<DumpMetainfo> dumpInfos) {
			foreach (DumpMetainfo dump in dumpInfos) {
				ScheduleDumpAnalysis(dump);
			}
		}

		public void ScheduleDumpAnalysis(DumpMetainfo dumpInfo) {
			string analysisWorkingDir = pathHelper.GetDumpDirectory(dumpInfo.Id);
			if (!Directory.Exists(analysisWorkingDir)) throw new DirectoryNotFoundException($"id: {dumpInfo.Id}");

			// schedule actual analysis
			var outgoingMessageTracer = dynatraceSdk.TraceOutgoingMessage(messagingSystemInfo);
			outgoingMessageTracer.Trace(() => {
				string jobId = Hangfire.BackgroundJob.Enqueue<AnalysisService>(repo => Analyze(dumpInfo, analysisWorkingDir, outgoingMessageTracer.GetDynatraceByteTag()));
				outgoingMessageTracer.SetVendorMessageId(jobId);
			});
		}

		[Hangfire.Queue("analysis", Order = 2)]
		public void Analyze(DumpMetainfo dumpInfo, string analysisWorkingDir, byte[] dynatraceTag = null) {
			var processTracer = dynatraceSdk.TraceIncomingMessageProcess(messagingSystemInfo);
			processTracer.SetDynatraceByteTag(dynatraceTag);
			processTracer.Trace(() => {
				AsyncHelper.RunSync(() => AnalyzeAsync(dumpInfo, analysisWorkingDir));
			});
		}

		public async Task AnalyzeAsync(DumpMetainfo dumpInfo, string analysisWorkingDir) {
			await bundleRepo.BlockIfBundleRepoNotReady($"AnalysisService.Analyze for {dumpInfo.Id}");

			dumpRepo.SetDumpStatus(dumpInfo.Id, DumpStatus.Analyzing, string.Empty);

			AnalyzerState state = AnalyzerState.Initialized;
			foreach (AnalyzerJob analyzerJob in analyzerPipeline.Analyzers) {
				if (state == AnalyzerState.Cancelled) {
					break;
				}
				state = await analyzerJob.AnalyzeDump(dumpInfo, analysisWorkingDir, state);
			}

			if (state == AnalyzerState.Failed || state == AnalyzerState.Initialized) {
				dumpRepo.SetDumpStatus(dumpInfo.Id, DumpStatus.Failed);
			} else {
				dumpRepo.SetDumpStatus(dumpInfo.Id, DumpStatus.Finished);

				dumpInfo = dumpRepo.Get(dumpInfo.Id);
				foreach(PostAnalysisJob job in analyzerPipeline.PostAnalysisJobs) {
					await job.AnalyzeDump(dumpInfo);
				}
			}

			await notifications.NotifyDumpAnalysisFinished(dumpInfo);
		}
	}
}
