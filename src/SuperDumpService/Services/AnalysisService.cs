using Microsoft.Extensions.Options;
using SuperDumpService.Helpers;
using SuperDumpService.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.Services {
	public class AnalysisService {
		private readonly DumpStorageFilebased dumpStorage;
		private readonly DumpRepository dumpRepo;
		private readonly PathHelper pathHelper;
		private readonly IOptions<SuperDumpSettings> settings;

		public AnalysisService(DumpStorageFilebased dumpStorage, DumpRepository dumpRepo, PathHelper pathHelper, IOptions<SuperDumpSettings> settings) {
			this.dumpStorage = dumpStorage;
			this.dumpRepo = dumpRepo;
			this.pathHelper = pathHelper;
			this.settings = settings;
		}

		public void ScheduleDumpAnalysis(DumpMetainfo dumpInfo) {
			string dumpFilePath = dumpStorage.GetDumpFilePath(dumpInfo.BundleId, dumpInfo.DumpId);
			if (!File.Exists(dumpFilePath)) throw new DumpNotFoundException($"bundleid: {dumpInfo.BundleId}, dumpid: {dumpInfo.DumpId}, path: {dumpFilePath}");

			string analysisWorkingDir = pathHelper.GetDumpDirectory(dumpInfo.BundleId, dumpInfo.DumpId);
			if (!Directory.Exists(analysisWorkingDir)) throw new DirectoryNotFoundException($"bundleid: {dumpInfo.BundleId}, dumpid: {dumpInfo.DumpId}, path: {dumpFilePath}");
			
			// schedule actual analysis
			Hangfire.BackgroundJob.Enqueue<AnalysisService>(repo => Analyze(dumpInfo, dumpFilePath, analysisWorkingDir));
		}

		[Hangfire.Queue("analysis", Order = 2)]
		public async Task Analyze(DumpMetainfo dumpInfo, string dumpFilePath, string analysisWorkingDir) {
			try {
				dumpRepo.SetDumpStatus(dumpInfo.BundleId, dumpInfo.DumpId, DumpStatus.Analyzing);
				string dumpselector = pathHelper.GetDumpSelectorExePath();

				Console.WriteLine($"launching '{dumpselector}' '{dumpFilePath}");
				using (var process = await ProcessRunner.Run(dumpselector, dumpFilePath)) {
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
				dumpRepo.SetDumpStatus(dumpInfo.BundleId, dumpInfo.DumpId, DumpStatus.Finished);
			} catch (OperationCanceledException e) {
				Console.WriteLine(e.Message);
				dumpRepo.SetDumpStatus(dumpInfo.BundleId, dumpInfo.DumpId, DumpStatus.Failed, e.ToString());
			} finally {
				if (settings.Value.DeleteDumpAfterAnalysis) {
					dumpStorage.DeleteDumpFile(dumpInfo.BundleId, dumpInfo.DumpId);
				}
			}
		}
	}
}
