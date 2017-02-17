using System;
using System.Collections.Generic;
using System.Linq;
using SuperDumpService.Models;
using System.IO;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using System.Net;
using System.Diagnostics;
using Hangfire;
using SuperDumpService.Helpers;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Threading.Tasks;
using SuperDump;
using SuperDumpService.Controllers;
using SuperDump.Models;

namespace SuperDumpService.Services {
	public class SuperDumpRepository {
		private IOptions<SuperDumpSettings> settings;

		private readonly BundleRepository bundleRepo;
		private readonly DumpRepository dumpRepo;
		private readonly AnalysisService analysisService;
		private readonly DownloadService downloadService;
		private readonly SymStoreService symStoreService;
		private readonly UnpackService unpackService;
		private readonly PathHelper pathHelper;

		public SuperDumpRepository(
				IOptions<SuperDumpSettings> settings,
				BundleRepository bundleRepo,
				DumpRepository dumpRepo,
				AnalysisService analysisService,
				DownloadService downloadService,
				SymStoreService symStoreService,
				UnpackService unpackService,
				PathHelper pathHelper) {
			this.settings = settings;
			this.bundleRepo = bundleRepo;
			this.dumpRepo = dumpRepo;
			this.analysisService = analysisService;
			this.downloadService = downloadService;
			this.symStoreService = symStoreService;
			this.unpackService = unpackService;
			this.pathHelper = pathHelper;
			pathHelper.PrepareDirectories();
		}

		public SDResult GetResult(string bundleId, string dumpId) {
			return dumpRepo.GetResult(bundleId, dumpId);
		}

		public bool ContainsBundle(string bundleId) {
			return bundleRepo.ContainsBundle(bundleId);
		}

		public BundleMetainfo GetBundle(string bundleId) {
			return bundleRepo.Get(bundleId);
		}

		public DumpMetainfo GetDump(string bundleId, string dumpId) {
			if (!string.IsNullOrEmpty(bundleId) && !string.IsNullOrEmpty(dumpId)) {
				return dumpRepo.Get(bundleId, dumpId);
			} else {
				return null;
			}
		}

		public void WipeAllExceptDump(string bundleId, string dumpId) {
			var dumpdir = pathHelper.GetDumpDirectory(bundleId, dumpId);
			var dumpfile = dumpRepo.GetDumpFilePath(bundleId, dumpId);
			foreach (var file in Directory.EnumerateFiles(dumpdir)) {
				if (file != dumpfile && file != pathHelper.GetDumpMetadataPath(bundleId, dumpId)) {
					File.Delete(file);
				}
			}
		}

		/// <summary>
		/// create bundle, process the file
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="input"></param>
		/// <returns>bundleId</returns>
		public string ProcessInputfile(string filename, DumpAnalysisInput input) {
			Console.WriteLine($"ProcessInputfile: {filename}");
			var bundleInfo = bundleRepo.Create(filename, input);
			ScheduleDownload(bundleInfo.BundleId, input.Url, filename); // indirectly calls ProcessFile()
			return bundleInfo.BundleId;
		}

		/// <summary>
		/// processes the file given.
		/// </summary>
		/// <param name="bundleId"></param>
		/// <param name="file"></param>
		/// <returns></returns>
		public async Task ProcessFile(string bundleId, FileInfo file) {
			Console.WriteLine($"ProcessFile: {file.FullName}");
			if (file.Name.EndsWith(".dmp", StringComparison.OrdinalIgnoreCase)) { await ProcessWindowsDump(bundleId, file); return; }
			if (file.Name.EndsWith(".core.gz", StringComparison.OrdinalIgnoreCase)) { await ProcessLinuxDump(bundleId, file); return; }
			if (file.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) { await ProcessZip(bundleId, file); return; }
			if (file.Name.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase)) { ProcessSymbol(file); return; }
			// ignore the file. it might still get picked up, if IncludeOtherFilesInReport is set.
		}

		private void ProcessSymbol(FileInfo file) {
			symStoreService.AddSymbols(file);
		}

		private async Task ProcessDirRecursive(string bundleId, DirectoryInfo dir) {
			Console.WriteLine($"ProcessDir: {dir.FullName}");
			var files = dir.GetFiles("*", SearchOption.AllDirectories); // don't use EnumerateFiles, because due to unzipping, files might be added
			foreach (FileInfo file in files) {
				await ProcessFile(bundleId, file);
			}
		}

		private async Task ProcessZip(string bundleId, FileInfo zipfile) {
			Console.WriteLine($"ProcessZip: {zipfile.FullName}");
			DirectoryInfo dir = unpackService.UnZip(zipfile);
			await ProcessDirRecursive(bundleId, dir);
		}

		private async Task ProcessWindowsDump(string bundleId, FileInfo file) {
			Console.WriteLine($"ProcessWindowsDump: {file.FullName}");
			// add dump
			var dumpInfo = await dumpRepo.CreateDump(bundleId, file);

			if (settings.Value.IncludeOtherFilesInReport) {
				var dir = file.Directory;
				foreach(var siblingFile in dir.EnumerateFiles()) {
					if (siblingFile.FullName == file.FullName) continue; // don't add actual dump file twice
					await dumpRepo.AddFileCopy(bundleId, dumpInfo.DumpId, siblingFile, SDFileType.SiblingFile);
				}
			}

			// schedule analysis
			analysisService.ScheduleDumpAnalysis(dumpInfo);
		}

		private async Task ProcessLinuxDump(string bundleId, FileInfo file) {
			Console.WriteLine($"ProcessLinuxDump: {file.FullName}");
			// add dump
			var dumpInfo = await dumpRepo.CreateDump(bundleId, file);

			// schedule analysis
			analysisService.ScheduleDumpAnalysis(dumpInfo);
		}

		private void ScheduleDownload(string bundleId, string url, string filename) {
			Hangfire.BackgroundJob.Enqueue<SuperDumpRepository>(repo => DownloadAndScheduleProcessFile(bundleId, url, filename));
		}

		[Hangfire.Queue("download", Order = 1)]
		public async Task DownloadAndScheduleProcessFile(string bundleId, string url, string filename) {
			bundleRepo.SetBundleStatus(bundleId, BundleStatus.Downloading);
			try {
				using (TempDirectoryHandle tempDir = await downloadService.Download(bundleId, url, filename)) {
					// this class should only do downloading. 
					// unf. i could not find a good way to *not* make this call from with DownloadService
					// hangfire supports continuations, but not parameterized. i found no way to pass the result (TempFileHandle) over to the continuation
					await ProcessDirRecursive(bundleId, tempDir.Dir);
				}
				bundleRepo.SetBundleStatus(bundleId, BundleStatus.Finished);
			} catch (Exception e) {
				bundleRepo.SetBundleStatus(bundleId, BundleStatus.Failed, e.ToString());
			}
		}

		public void RerunAnalysis(string bundleId, string dumpId) {
			WipeAllExceptDump(bundleId, dumpId);
			var dumpInfo = dumpRepo.Get(bundleId, dumpId);
			analysisService.ScheduleDumpAnalysis(dumpInfo);
		}
	}
}