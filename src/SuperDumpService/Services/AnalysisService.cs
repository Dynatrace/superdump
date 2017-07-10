﻿using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SuperDump.Common;
using SuperDumpService.Helpers;
using SuperDumpService.Models;
using System.Diagnostics;
using SuperDump.Models;

namespace SuperDumpService.Services {
	public class AnalysisService {
		private readonly DumpStorageFilebased dumpStorage;
		private readonly DumpRepository dumpRepo;
		private readonly BundleRepository bundleRepo;
		private readonly PathHelper pathHelper;
		private readonly IOptions<SuperDumpSettings> settings;
		private readonly NotificationService notifications;
		private readonly ElasticSearchService elasticSearch;

		public AnalysisService(DumpStorageFilebased dumpStorage, DumpRepository dumpRepo, BundleRepository bundleRepo, PathHelper pathHelper, IOptions<SuperDumpSettings> settings, NotificationService notifications, ElasticSearchService elasticSearch) {
			this.dumpStorage = dumpStorage;
			this.dumpRepo = dumpRepo;
			this.bundleRepo = bundleRepo;
			this.pathHelper = pathHelper;
			this.settings = settings;
			this.notifications = notifications;
			this.elasticSearch = elasticSearch;
		}

		public void ScheduleDumpAnalysis(DumpMetainfo dumpInfo) {
			string dumpFilePath = dumpStorage.GetDumpFilePath(dumpInfo.BundleId, dumpInfo.DumpId);
			if (!File.Exists(dumpFilePath)) throw new DumpNotFoundException($"bundleid: {dumpInfo.BundleId}, dumpid: {dumpInfo.DumpId}, path: {dumpFilePath}");

			string analysisWorkingDir = pathHelper.GetDumpDirectory(dumpInfo.BundleId, dumpInfo.DumpId);
			if (!Directory.Exists(analysisWorkingDir)) throw new DirectoryNotFoundException($"bundleid: {dumpInfo.BundleId}, dumpid: {dumpInfo.DumpId}, path: {dumpFilePath}");

			// schedule actual analysis
			Hangfire.BackgroundJob.Enqueue<AnalysisService>(repo => Analyze(dumpInfo, dumpFilePath, analysisWorkingDir)); // got a stackoverflow problem here.
		}

		[Hangfire.Queue("analysis", Order = 2)]
		public async Task Analyze(DumpMetainfo dumpInfo, string dumpFilePath, string analysisWorkingDir) {
			try {
				dumpRepo.SetDumpStatus(dumpInfo.BundleId, dumpInfo.DumpId, DumpStatus.Analyzing);

				if (dumpInfo.DumpType == DumpType.WindowsDump) {
					await AnalyzeWindows(dumpInfo, new DirectoryInfo(analysisWorkingDir), dumpFilePath);
				} else if (dumpInfo.DumpType == DumpType.LinuxCoreDump) {
					await LinuxAnalyzationAsync(dumpInfo, new DirectoryInfo(analysisWorkingDir), dumpFilePath);
				} else {
					throw new Exception("unknown dumptype. here be dragons");
				}
				dumpRepo.SetDumpStatus(dumpInfo.BundleId, dumpInfo.DumpId, DumpStatus.Finished);

				SDResult result = dumpRepo.GetResult(dumpInfo.BundleId, dumpInfo.DumpId, out string err);
				if (result != null) {
					var bundle = bundleRepo.Get(dumpInfo.BundleId);
					await elasticSearch.PushResultAsync(result, bundle, dumpInfo);
				}
			} catch (Exception e) {
				Console.WriteLine(e.Message);
				dumpRepo.SetDumpStatus(dumpInfo.BundleId, dumpInfo.DumpId, DumpStatus.Failed, e.ToString());
			} finally {
				if (settings.Value.DeleteDumpAfterAnalysis) {
					dumpStorage.DeleteDumpFile(dumpInfo.BundleId, dumpInfo.DumpId);
				}
				await notifications.NotifyDumpAnalysisFinished(dumpInfo);
			}
		}

		private async Task AnalyzeWindows(DumpMetainfo dumpInfo, DirectoryInfo workingDir, string dumpFilePath) {
			string dumpselector = pathHelper.GetDumpSelectorExePath();

			Console.WriteLine($"launching '{dumpselector}' '{dumpFilePath}'");
			using (var process = await ProcessRunner.Run(dumpselector, workingDir, WrapInQuotes(dumpFilePath), WrapInQuotes(pathHelper.GetJsonPath(dumpInfo.BundleId, dumpInfo.DumpId)))) {
				string selectorLog = $"SuperDumpSelector exited with error code {process.ExitCode}" +
					$"{Environment.NewLine}{Environment.NewLine}stdout:{Environment.NewLine}{process.StdOut}" +
					$"{Environment.NewLine}{Environment.NewLine}stderr:{Environment.NewLine}{process.StdErr}";
				Console.WriteLine(selectorLog);
				File.WriteAllText(Path.Combine(pathHelper.GetDumpDirectory(dumpInfo.BundleId, dumpInfo.DumpId), "superdumpselector.log"), selectorLog);
				if (process.ExitCode != 0) {
					dumpRepo.SetDumpStatus(dumpInfo.BundleId, dumpInfo.DumpId, DumpStatus.Failed, selectorLog);
					throw new Exception(selectorLog);
				}
			}

			await RunDebugDiagAnalysis(dumpInfo, workingDir, dumpFilePath);
		}

		private static string WrapInQuotes(string str) {
			return $"\"{str}\"";
		}

		private async Task RunDebugDiagAnalysis(DumpMetainfo dumpInfo, DirectoryInfo workingDir, string dumpFilePath) {
			//--dump = "C:\superdump\data\dumps\hno3391\iwb0664\iwb0664.dmp"--out= "C:\superdump\data\dumps\hno3391\iwb0664\debugdiagout.mht"--symbolPath = "cache*c:\localsymbols;http://msdl.microsoft.com/download/symbols"--overwrite
			string reportFilePath = Path.Combine(pathHelper.GetDumpDirectory(dumpInfo.BundleId, dumpInfo.DumpId), "DebugDiagAnalysis.mht");
			string debugDiagExe = "SuperDump.DebugDiag.exe";

			try {
				using (var process = await ProcessRunner.Run(debugDiagExe, workingDir,
					$"--dump=\"{dumpFilePath}\"",
					$"--out=\"{reportFilePath}\"",
					"--overwrite")) {
					string log = $"debugDiagExe exited with error code {process.ExitCode}" +
						$"{Environment.NewLine}{Environment.NewLine}stdout:{Environment.NewLine}{process.StdOut}" +
						$"{Environment.NewLine}{Environment.NewLine}stderr:{Environment.NewLine}{process.StdErr}";
					Console.WriteLine(log);
					File.WriteAllText(Path.Combine(pathHelper.GetDumpDirectory(dumpInfo.BundleId, dumpInfo.DumpId), "superdump.debugdiag.log"), log);
					dumpRepo.AddFile(dumpInfo.BundleId, dumpInfo.DumpId, "DebugDiagAnalysis.mht", SDFileType.DebugDiagResult);
				}
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
			command = command.Replace("{outputpath}", pathHelper.GetJsonPath(dumpInfo.BundleId, dumpInfo.DumpId));
			command = command.Replace("{outputname}", Path.GetFileName(pathHelper.GetJsonPath(dumpInfo.BundleId, dumpInfo.DumpId)));

			Utility.ExtractExe(command, out string executable, out string arguments);

			Console.WriteLine($"running exe='{executable}', args='{arguments}'");
			using (var process = await ProcessRunner.Run(executable, workingDir, arguments)) {
				Console.WriteLine($"stdout: {process.StdOut}");
				Console.WriteLine($"stderr: {process.StdErr}");

				if (process.StdOut?.Length > 0) {
					File.WriteAllText(Path.Combine(pathHelper.GetDumpDirectory(dumpInfo.BundleId, dumpInfo.DumpId), "linux-analysis.log"), process.StdOut);
				}
				if (process.StdErr?.Length > 0) {
					File.WriteAllText(Path.Combine(pathHelper.GetDumpDirectory(dumpInfo.BundleId, dumpInfo.DumpId), "linux-analysis.err.log"), process.StdErr);
				}

				if (process.ExitCode != 0) {
					string error = $"Analysis failed with exit code {process.ExitCode}. See log files for more details.";
					Console.WriteLine(error);
					throw new Exception(error);
				}
			}
		}
	}
}
