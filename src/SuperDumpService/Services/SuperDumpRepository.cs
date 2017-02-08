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

		public SuperDumpRepository(
				IOptions<SuperDumpSettings> settings,
				BundleRepository bundleRepo,
				DumpRepository dumpRepo,
				AnalysisService analysisService,
				DownloadService downloadService,
				SymStoreService symStoreService,
				UnpackService unpackService) {
			this.settings = settings;
			this.bundleRepo = bundleRepo;
			this.dumpRepo = dumpRepo;
			this.analysisService = analysisService;
			this.downloadService = downloadService;
			this.symStoreService = symStoreService;
			this.unpackService = unpackService;
			PathHelper.PrepareDirectories();
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
			var dumpdir = PathHelper.GetDumpDirectory(bundleId, dumpId);
			var dumpfile = PathHelper.GetDumpfilePath(bundleId, dumpId);
			foreach (var file in Directory.EnumerateFiles(dumpdir)) {
				if (file != dumpfile && file != PathHelper.GetDumpMetadataPath(bundleId, dumpId)) {
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
			var extension = file.Extension.ToLower();
			switch (extension) {
				case ".dmp":
					await ProcessDump(bundleId, file);
					break;
				case ".zip":
					await ProcessZip(bundleId, file);
					break;
				case ".pdb":
					ProcessSymbol(file);
					break;
				default:
					throw new InvalidDataException($"filetype '{extension}' not supported");
			}
		}

		private void ProcessSymbol(FileInfo file) {
			symStoreService.AddSymbols(file);
		}

		private async Task ProcessDir(string bundleId, DirectoryInfo dir) {
			foreach (FileInfo file in dir.EnumerateFiles()) {
				await ProcessFile(bundleId, file);
			}
			foreach (DirectoryInfo subdir in dir.EnumerateDirectories()) {
				await ProcessDir(bundleId, subdir);
			}
		}

		private async Task ProcessZip(string bundleId, FileInfo zipfile) {
			using (TempDirectoryHandle dir = unpackService.UnZip(zipfile, filename => {
				var ext = Path.GetExtension(filename).ToLower();
				return ext == ".dmp" || ext == ".zip" || ext == ".pdb";
			})) {
				await ProcessDir(bundleId, dir.Dir);
			}
		}

		private async Task ProcessDump(string bundleId, FileInfo file) {
			// add dump
			var dumpInfo = await dumpRepo.AddDump(bundleId, file);

			// schedule analysis
			analysisService.ScheduleDumpAnalysis(dumpInfo);
		}

		private void ScheduleDownload(string bundleId, string url, string filename) {
			Hangfire.BackgroundJob.Enqueue<SuperDumpRepository>(repo => DownloadAndScheduleProcessFile(bundleId, url, filename));
		}

		[Hangfire.Queue("download", Order = 1)]
		public async Task DownloadAndScheduleProcessFile(string bundleId, string url, string filename) {
			bundleRepo.SetBundleStatus(bundleId, BundleStatus.Downloading);
			using (var tempFile = await downloadService.Download(bundleId, url, filename)) {
				// this class should only do downloading. 
				// unf. i could not find a good way to *not* make this call from with DownloadService
				// hangfire supports continuations, but not parameterized. i found no way to pass the result (TempFileHandle) over to the continuation
				await ProcessFile(bundleId, tempFile.File);
			}
			bundleRepo.SetBundleStatus(bundleId, BundleStatus.Finished);
		}

		public void RerunAnalysis(string bundleId, string dumpId) {
			WipeAllExceptDump(bundleId, dumpId);
			var dumpInfo = dumpRepo.Get(bundleId, dumpId);
			analysisService.ScheduleDumpAnalysis(dumpInfo);
		}
	}
}