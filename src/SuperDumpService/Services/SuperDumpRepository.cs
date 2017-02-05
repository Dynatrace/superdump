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

		public SuperDumpRepository(IOptions<SuperDumpSettings> settings, BundleRepository bundleRepo, DumpRepository dumpRepo, AnalysisService analysisService, DownloadService downloadService, SymStoreService symStoreService) {
			this.settings = settings;
			this.bundleRepo = bundleRepo;
			this.dumpRepo = dumpRepo;
			this.analysisService = analysisService;
			this.downloadService = downloadService;
			this.symStoreService = symStoreService;
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

		public IEnumerable<string> GetReportFileNames(string bundleId, string id) {
			foreach (var file in Directory.EnumerateFiles(PathHelper.GetDumpDirectory(bundleId, id))) {
				yield return new FileInfo(file).Name;
			}
		}

		public FileInfo GetReportFile(string bundleId, string id, string filename) {
			var f = new FileInfo(Path.Combine(PathHelper.GetDumpDirectory(bundleId, id), filename));
			if (f.Exists) {
				return f;
			} else {
				return null;
			}
		}

		public void WipeAllExceptDump(string bundleId, string dumpId) {
			var dumpdir = PathHelper.GetDumpDirectory(bundleId, dumpId);
			var dumpfile = PathHelper.GetDumpfilePath(bundleId, dumpId);
			foreach (var file in Directory.EnumerateFiles(dumpdir)) {
				if (file != dumpfile) {
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
			var bundleInfo = bundleRepo.Create();
			ScheduleDownload(bundleInfo.BundleId, input.Url, filename); // indirectly calls ProcessFile()
			return bundleInfo.BundleId;
		}

		/// <summary>
		/// processes the file given.
		/// </summary>
		/// <param name="bundleId"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		public async Task ProcessFile(string bundleId, string path) {
			var extension = Path.GetExtension(path).ToLower();
			switch (extension) {
				case ".dmp":
					await ProcessDump(bundleId, path);
					break;
				case ".zip":
					await ProcessZip(bundleId, path);
					break;
				case ".dll":
				case ".pdb":
					await ProcessSymbol(path);
					break;
				default:
					throw new InvalidDataException($"filetype '{extension}' not supported");
			}
		}

		private async Task ProcessSymbol(string path) {
			await symStoreService.AddSymbols(path);
		}

		private async Task ProcessZip(string bundleId, string path) {
			var zipItems = Utility.UnzipDumpZip(path);

			if (!zipItems.Any()) {
				// TODO: proper error
				throw new NotImplementedException();
			}

			foreach (var item in zipItems) {
				await ProcessFile(bundleId, item);
			}
			// TODO delete unpacked files!
		}

		private async Task ProcessDump(string bundleId, string path) {
			// add dump
			var dumpInfo = await dumpRepo.AddDump(bundleId, path);

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
				await ProcessFile(bundleId, tempFile.Path);
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