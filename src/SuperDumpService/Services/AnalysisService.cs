using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SuperDump.Common;
using SuperDumpService.Helpers;
using SuperDumpService.Models;
using System.Diagnostics;

namespace SuperDumpService.Services {
	public class AnalysisService {
		private readonly DumpStorageFilebased dumpStorage;
		private readonly DumpRepository dumpRepo;
		private readonly PathHelper pathHelper;
		private readonly IOptions<SuperDumpSettings> settings;
		private readonly NotificationService notifications;

		public AnalysisService(DumpStorageFilebased dumpStorage, DumpRepository dumpRepo, PathHelper pathHelper, IOptions<SuperDumpSettings> settings, NotificationService notifications) {
			this.dumpStorage = dumpStorage;
			this.dumpRepo = dumpRepo;
			this.pathHelper = pathHelper;
			this.settings = settings;
			this.notifications = notifications;
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
					StartLinuxAnalyzation(dumpInfo, new DirectoryInfo(analysisWorkingDir), dumpFilePath);
				} else {
					throw new Exception("unknown dumptype. here be dragons");
				}
				dumpRepo.SetDumpStatus(dumpInfo.BundleId, dumpInfo.DumpId, DumpStatus.Finished);
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

		private void StartLinuxAnalyzation(DumpMetainfo dumpInfo, DirectoryInfo workingDir, string dumpFilePath) {
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
			/* Using Process implementation because bash cannot be started with RedirectStandardOutput=true (which is hardcoded in ProcessRunner)
			using (var process = await ProcessRunner.Run(executable, workingDir, arguments)) {
				File.WriteAllText(Path.Combine(workingDir.FullName, "linux-analysis.txt"), process.StdOut);
				dumpRepo.AddFile(dumpInfo.BundleId, dumpInfo.DumpId, "linux-analysis.txt", SDFileType.CustomTextResult);
			}*/
			ProcessStartInfo psi = new ProcessStartInfo(executable, arguments) {
				WorkingDirectory = workingDir.FullName
			};
			Process.Start(psi);
		}
	}
}
