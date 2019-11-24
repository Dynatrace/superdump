using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Dynatrace.OneAgent.Sdk.Api;
using Dynatrace.OneAgent.Sdk.Api.Enums;
using Microsoft.Extensions.Options;
using SuperDump.Common;
using SuperDump.Models;
using SuperDumpService.Helpers;
using SuperDumpService.Models;

namespace SuperDumpService.Services.Analyzers {
	public class DumpAnalyzerJob : InitalAnalyzerJob {
		private readonly DumpRepository dumpRepo;
		private readonly IOptions<SuperDumpSettings> settings;
		private readonly PathHelper pathHelper;
		private readonly IOneAgentSdk dynatraceSdk;

		public DumpAnalyzerJob(
				DumpRepository dumpRepo,
				IOptions<SuperDumpSettings> settings,
				PathHelper pathHelper, 
				IOneAgentSdk dynatraceSdk) {
			this.dumpRepo = dumpRepo;
			this.settings = settings;
			this.pathHelper = pathHelper;
			this.dynatraceSdk = dynatraceSdk;
		}

		public override async Task<IEnumerable<DumpMetainfo>> CreateDumpInfos(string bundleId, DirectoryInfo directory) {
			var dumps = new List<DumpMetainfo>();
			foreach (FileInfo file in directory.GetFiles()) {
				if (file.Name.EndsWith(".dmp", StringComparison.OrdinalIgnoreCase)) {
					dumps.Add(await dumpRepo.CreateDump(bundleId, file, DumpType.WindowsDump));
				} else if (file.Name.EndsWith(".core.gz", StringComparison.OrdinalIgnoreCase)) {
					dumps.Add(await dumpRepo.CreateDump(bundleId, file, DumpType.LinuxCoreDump));
				} else if (file.Name.EndsWith(".core", StringComparison.OrdinalIgnoreCase)) {
					dumps.Add(await dumpRepo.CreateDump(bundleId, file, DumpType.LinuxCoreDump));
				}
			}
			return dumps;
		}

		public override async Task<AnalyzerState> AnalyzeDump(DumpMetainfo dumpInfo, string analysisWorkingDir, AnalyzerState previousState) {
			if (dumpInfo.DumpType != DumpType.WindowsDump && dumpInfo.DumpType != DumpType.LinuxCoreDump) {
				return AnalyzerState.Failed;
			}

			try {
				string dumpFilePath = dumpRepo.GetDumpFilePath(dumpInfo.Id);
				if (!File.Exists(dumpFilePath)) {
					dumpRepo.SetErrorMessage(dumpInfo.Id, $"Primary dump file not found (id: {dumpInfo.Id}, path: {dumpFilePath})");
					return AnalyzerState.Failed;
				}

				if (new FileInfo(dumpFilePath).Length == 0) {
					dumpRepo.SetErrorMessage(dumpInfo.Id, "The primary dump file is empty!");
					return AnalyzerState.Failed;
				}

				if (dumpInfo.DumpType == DumpType.WindowsDump) {
					await AnalyzeWindows(dumpInfo, new DirectoryInfo(analysisWorkingDir), dumpFilePath);
				} else if (dumpInfo.DumpType == DumpType.LinuxCoreDump) {
					await LinuxAnalyzationAsync(dumpInfo, new DirectoryInfo(analysisWorkingDir), dumpFilePath);
				}

				// Re-fetch dump info as it was updated
				dumpInfo = dumpRepo.Get(dumpInfo.Id);

				SDResult result = await dumpRepo.GetResultAndThrow(dumpInfo.Id);

				if (result != null) {
					return AnalyzerState.Succeeded;
				} else {
					return AnalyzerState.Failed;
				}
			} catch (Exception e) {
				Console.WriteLine(e.Message);
				dumpRepo.SetErrorMessage(dumpInfo.Id, e.ToString());
				return AnalyzerState.Failed;
			} finally {
				dumpInfo = dumpRepo.Get(dumpInfo.Id);
				if (settings.Value.DeleteDumpAfterAnalysis) {
					dumpRepo.DeleteDumpFile(dumpInfo.Id);
				}
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
	}
}
