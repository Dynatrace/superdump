using System;
using System.IO;
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

namespace SuperDumpService.Services {
	public class AnalysisService {
		private readonly IDumpStorage dumpStorage;
		private readonly DumpRepository dumpRepo;
		private readonly BundleRepository bundleRepo;
		private readonly PathHelper pathHelper;
		private readonly IOptions<SuperDumpSettings> settings;
		private readonly NotificationService notifications;
		private readonly ElasticSearchService elasticSearch;
		private readonly SimilarityService similarityService;

		private readonly IOneAgentSdk dynatraceSdk;
		private readonly IMessagingSystemInfo messagingSystemInfo;

		public AnalysisService(
					IDumpStorage dumpStorage,
					DumpRepository dumpRepo,
					BundleRepository bundleRepo,
					PathHelper pathHelper,
					IOptions<SuperDumpSettings> settings,
					NotificationService notifications,
					ElasticSearchService elasticSearch,
					SimilarityService similarityService,
					IOneAgentSdk dynatraceSdk
				) {
			this.dumpStorage = dumpStorage;
			this.dumpRepo = dumpRepo;
			this.bundleRepo = bundleRepo;
			this.pathHelper = pathHelper;
			this.settings = settings;
			this.notifications = notifications;
			this.elasticSearch = elasticSearch;
			this.similarityService = similarityService;
			this.dynatraceSdk = dynatraceSdk;
			messagingSystemInfo = dynatraceSdk.CreateMessagingSystemInfo("Hangfire", "analysis", MessageDestinationType.QUEUE, ChannelType.IN_PROCESS, null);
		}

		public void ScheduleDumpAnalysis(DumpMetainfo dumpInfo) {
			string dumpFilePath = dumpStorage.GetDumpFilePath(dumpInfo.Id);
			if (!File.Exists(dumpFilePath)) throw new DumpNotFoundException($"id: {dumpInfo.Id}, path: {dumpFilePath}");

			string analysisWorkingDir = pathHelper.GetDumpDirectory(dumpInfo.Id);
			if (!Directory.Exists(analysisWorkingDir)) throw new DirectoryNotFoundException($"id: {dumpInfo.Id}, path: {dumpFilePath}");

			// schedule actual analysis
			var outgoingMessageTracer = dynatraceSdk.TraceOutgoingMessage(messagingSystemInfo);
			outgoingMessageTracer.Trace(() => {
				string jobId = Hangfire.BackgroundJob.Enqueue<AnalysisService>(repo => Analyze(dumpInfo, dumpFilePath, analysisWorkingDir, outgoingMessageTracer.GetDynatraceByteTag()));
				outgoingMessageTracer.SetVendorMessageId(jobId);
			});
		}

		[Hangfire.Queue("analysis", Order = 2)]
		public void Analyze(DumpMetainfo dumpInfo, string dumpFilePath, string analysisWorkingDir, byte[] dynatraceTag = null) {
			var processTracer = dynatraceSdk.TraceIncomingMessageProcess(messagingSystemInfo);
			processTracer.SetDynatraceByteTag(dynatraceTag);
			processTracer.Trace(() => {
				AsyncHelper.RunSync(() => AnalyzeAsync(dumpInfo, dumpFilePath, analysisWorkingDir));
			});
		}

		public async Task AnalyzeAsync(DumpMetainfo dumpInfo, string dumpFilePath, string analysisWorkingDir) {
			await BlockIfBundleRepoNotReady($"AnalysisService.Analyze for {dumpInfo.Id}");

			try {
				dumpRepo.SetDumpStatus(dumpInfo.Id, DumpStatus.Analyzing);

				if (dumpInfo.DumpType == DumpType.WindowsDump) {
					await AnalyzeWindows(dumpInfo, new DirectoryInfo(analysisWorkingDir), dumpFilePath);
				} else if (dumpInfo.DumpType == DumpType.LinuxCoreDump) {
					await LinuxAnalyzationAsync(dumpInfo, new DirectoryInfo(analysisWorkingDir), dumpFilePath);
				} else {
					throw new Exception("unknown dumptype. here be dragons");
				}

				// Re-fetch dump info as it was updated
				dumpInfo = dumpRepo.Get(dumpInfo.Id);

				SDResult result = await dumpRepo.GetResultAndThrow(dumpInfo.Id);

				if (result != null) {
					dumpRepo.WriteResult(dumpInfo.Id, result);
					dumpRepo.SetDumpStatus(dumpInfo.Id, DumpStatus.Finished);

					var bundle = bundleRepo.Get(dumpInfo.BundleId);
					await elasticSearch.PushResultAsync(result, bundle, dumpInfo);
				}
			} catch (Exception e) {
				Console.WriteLine(e.Message);
				dumpRepo.SetDumpStatus(dumpInfo.Id, DumpStatus.Failed, e.ToString());
			} finally {
				if (settings.Value.DeleteDumpAfterAnalysis) {
					dumpStorage.DeleteDumpFile(dumpInfo.Id);
				}
				await notifications.NotifyDumpAnalysisFinished(dumpInfo);
				similarityService.ScheduleSimilarityAnalysis(dumpInfo, false, DateTime.Now - TimeSpan.FromDays(settings.Value.SimilarityDetectionMaxDays));
			}
		}

		private async Task AnalyzeWindows(DumpMetainfo dumpInfo, DirectoryInfo workingDir, string dumpFilePath) {
			string dumpselector = "SuperDumpSelector.exe"; // should be on PATH
			var tracer = dynatraceSdk.TraceOutgoingRemoteCall("Analyze", dumpselector, dumpselector, ChannelType.OTHER, dumpselector);

			await tracer.TraceAsync(async () => {
				Console.WriteLine($"launching '{dumpselector}' '{dumpFilePath}'");
				string[] arguments = {
					$"--dump \"{dumpFilePath}\"",
					$"--out \"{pathHelper.GetJsonPath(dumpInfo.Id)}\"",
					$"--tracetag \"{tracer.GetDynatraceStringTag()}\""
				};
				using (var process = await ProcessRunner.Run(dumpselector, workingDir, arguments)) {
					string selectorLog = $"SuperDumpSelector exited with error code {process.ExitCode}" +
						$"{Environment.NewLine}{Environment.NewLine}stdout:{Environment.NewLine}{process.StdOut}" +
						$"{Environment.NewLine}{Environment.NewLine}stderr:{Environment.NewLine}{process.StdErr}";
					Console.WriteLine(selectorLog);
					File.WriteAllText(Path.Combine(pathHelper.GetDumpDirectory(dumpInfo.Id), "superdumpselector.log"), selectorLog);
					if (process.ExitCode != 0) {
						dumpRepo.SetDumpStatus(dumpInfo.Id, DumpStatus.Failed, selectorLog);
						throw new Exception(selectorLog);
					}
				}
			});

			await RunDebugDiagAnalysis(dumpInfo, workingDir, dumpFilePath);
		}

		private async Task RunDebugDiagAnalysis(DumpMetainfo dumpInfo, DirectoryInfo workingDir, string dumpFilePath) {
			//--dump = "C:\superdump\data\dumps\hno3391\iwb0664\iwb0664.dmp"--out= "C:\superdump\data\dumps\hno3391\iwb0664\debugdiagout.mht"--symbolPath = "cache*c:\localsymbols;http://msdl.microsoft.com/download/symbols"--overwrite
			string reportFilePath = Path.Combine(pathHelper.GetDumpDirectory(dumpInfo.Id), "DebugDiagAnalysis.mht");
			string debugDiagExe = "SuperDump.DebugDiag.exe";

			var tracer = dynatraceSdk.TraceOutgoingRemoteCall("Analyze", debugDiagExe, debugDiagExe, ChannelType.OTHER, debugDiagExe);
			try {
				await tracer.TraceAsync(async () => {
					using (var process = await ProcessRunner.Run(debugDiagExe, workingDir,
						$"--dump=\"{dumpFilePath}\"",
						$"--out=\"{reportFilePath}\"",
						"--overwrite",
						$"--tracetag \"{tracer.GetDynatraceStringTag()}\""
					)) {
						string log = $"debugDiagExe exited with error code {process.ExitCode}" +
							$"{Environment.NewLine}{Environment.NewLine}stdout:{Environment.NewLine}{process.StdOut}" +
							$"{Environment.NewLine}{Environment.NewLine}stderr:{Environment.NewLine}{process.StdErr}";
						Console.WriteLine(log);
						File.WriteAllText(Path.Combine(pathHelper.GetDumpDirectory(dumpInfo.Id), "superdump.debugdiag.log"), log);
						dumpRepo.AddFile(dumpInfo.Id, "DebugDiagAnalysis.mht", SDFileType.DebugDiagResult);
					}
				});
			} catch (ProcessRunnerException e) {
				if (e.InnerException is FileNotFoundException) {
					Console.Error.WriteLine($"{debugDiagExe} not found. Check BinPath setting in appsettings.json.");
				} else {
					Console.Error.WriteLine($"Error during DebugDiag analyis: {e}");
				}
				// do not abort analysis.
			}
		}

		private async Task LinuxAnalyzationAsync(DumpMetainfo dumpInfo, DirectoryInfo workingDir, string dumpFilePath) {
			string command = settings.Value.LinuxAnalysisCommand;

			if (string.IsNullOrEmpty(command)) {
				throw new ArgumentNullException("'LinuxCommandTemplate' setting is not configured.");
			}

			command = command.Replace("{bundleid}", dumpInfo.BundleId);
			command = command.Replace("{dumpid}", dumpInfo.DumpId);
			command = command.Replace("{dumpdir}", workingDir.FullName);
			command = command.Replace("{dumppath}", dumpFilePath);
			command = command.Replace("{dumpname}", Path.GetFileName(dumpFilePath));
			command = command.Replace("{outputpath}", pathHelper.GetJsonPath(dumpInfo.Id));
			command = command.Replace("{outputname}", Path.GetFileName(pathHelper.GetJsonPath(dumpInfo.Id)));

			Utility.ExtractExe(command, out string executable, out string arguments);

			Console.WriteLine($"running exe='{executable}', args='{arguments}'");
			using (var process = await ProcessRunner.Run(executable, workingDir, arguments)) {
				Console.WriteLine($"stdout: {process.StdOut}");
				Console.WriteLine($"stderr: {process.StdErr}");

				if (process.StdOut?.Length > 0) {
					File.WriteAllText(Path.Combine(pathHelper.GetDumpDirectory(dumpInfo.Id), "linux-analysis.log"), process.StdOut);
				}
				if (process.StdErr?.Length > 0) {
					File.WriteAllText(Path.Combine(pathHelper.GetDumpDirectory(dumpInfo.Id), "linux-analysis.err.log"), process.StdErr);
				}

				if (process.ExitCode != 0) {
					LinuxAnalyzerExitCode exitCode = (LinuxAnalyzerExitCode)process.ExitCode;
					string error = $"Exit code {process.ExitCode}: {exitCode.Message}";
					Console.WriteLine(error);
					throw new Exception(error);
				}
			}
		}

		/// <summary>
		/// Blocks until bundleRepo is fully populated.
		/// </summary>
		private async Task BlockIfBundleRepoNotReady(string sourcemethod) {
			if (!bundleRepo.IsPopulated) {
				Console.WriteLine($"{sourcemethod} is blocked because dumpRepo is not yet fully populated...");
				await Utility.BlockUntil(() => bundleRepo.IsPopulated);
				Console.WriteLine($"...continuing {sourcemethod}.");
			}
		}
	}
}
