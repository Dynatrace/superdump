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
using Dynatrace.OneAgent.Sdk.Api;
using Dynatrace.OneAgent.Sdk.Api.Infos;
using Dynatrace.OneAgent.Sdk.Api.Enums;

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
		private readonly IdenticalDumpRepository identicalRepository;

		private readonly IOneAgentSdk dynatraceSdk;
		private readonly IMessagingSystemInfo messagingSystemInfo;

		public SuperDumpRepository(
				IOptions<SuperDumpSettings> settings,
				BundleRepository bundleRepo,
				DumpRepository dumpRepo,
				AnalysisService analysisService,
				DownloadService downloadService,
				SymStoreService symStoreService,
				UnpackService unpackService,
				PathHelper pathHelper,
				IdenticalDumpRepository identicalRepository,
				IOneAgentSdk dynatraceSdk) {
			this.settings = settings;
			this.bundleRepo = bundleRepo;
			this.dumpRepo = dumpRepo;
			this.analysisService = analysisService;
			this.downloadService = downloadService;
			this.symStoreService = symStoreService;
			this.unpackService = unpackService;
			this.pathHelper = pathHelper;
			this.identicalRepository = identicalRepository;
			pathHelper.PrepareDirectories();

			this.dynatraceSdk = dynatraceSdk;
			messagingSystemInfo = dynatraceSdk.CreateMessagingSystemInfo("Hangfire", "download", MessageDestinationType.QUEUE, ChannelType.IN_PROCESS, null);
		}

		public async Task<SDResult> GetResultAndThrow(DumpIdentifier id) {
			return await dumpRepo.GetResultAndThrow(id);

		}
		public async Task<SDResult> GetResult(DumpIdentifier id) {
			return await dumpRepo.GetResult(id);
		}

		public bool ContainsBundle(string bundleId) {
			return bundleRepo.ContainsBundle(bundleId);
		}

		public BundleMetainfo GetBundle(string bundleId) {
			return bundleRepo.Get(bundleId);
		}

		public DumpMetainfo GetDump(DumpIdentifier id) {
			if (!string.IsNullOrEmpty(id.BundleId) && !string.IsNullOrEmpty(id.DumpId)) {
				return dumpRepo.Get(id);
			} else {
				return null;
			}
		}

		public void WipeAllExceptDump(DumpIdentifier id) {
			var dumpdir = pathHelper.GetDumpDirectory(id);
			var knownfiles = dumpRepo.GetFileNames(id);
			foreach (var file in Directory.EnumerateFiles(dumpdir)) {
				bool shallDelete = true;
				var match = knownfiles.SingleOrDefault(x => x.FileInfo.FullName == file);
				if (match != null) {
					shallDelete = match.FileEntry.Type == SDFileType.SuperDumpData
						|| match.FileEntry.Type == SDFileType.SuperDumpLogfile
						|| match.FileEntry.Type == SDFileType.DebugDiagResult
						|| match.FileEntry.Type == SDFileType.CustomTextResult
						|| match.FileEntry.Type == SDFileType.WinDbg;
				}
				if (shallDelete) {
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
			if (file.Name.EndsWith(".dmp", StringComparison.OrdinalIgnoreCase)) { await ProcessDump(bundleId, file); return; }
			if (file.Name.EndsWith(".core.gz", StringComparison.OrdinalIgnoreCase)) { await ProcessDump(bundleId, file); return; }
			if (file.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) { await ProcessZip(bundleId, file); return; }
			if (file.Name.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase)) { ProcessSymbol(file); return; }
			// ignore the file. it might still get picked up, if IncludeOtherFilesInReport is set.
		}

		private void ProcessSymbol(FileInfo file) {
			symStoreService.AddSymbols(file);
		}

		private async Task ProcessDirRecursive(string bundleId, DirectoryInfo dir) {
			var files = dir.GetFiles("*", SearchOption.AllDirectories); // don't use EnumerateFiles, because due to unzipping, files might be added
			foreach (FileInfo file in files) {
				await ProcessFile(bundleId, file);
			}
		}

		private async Task ProcessZip(string bundleId, FileInfo zipfile) {
			DirectoryInfo dir = unpackService.UnZip(zipfile);
			await ProcessDirRecursive(bundleId, dir);
		}

		private async Task ProcessDump(string bundleId, FileInfo file) {
			// add dump
			var dumpInfo = await dumpRepo.CreateDump(bundleId, file);

			// add other files within the same directory
			await IncludeOtherFiles(bundleId, file, dumpInfo);

			// schedule analysis
			analysisService.ScheduleDumpAnalysis(dumpInfo);
		}

		private async Task IncludeOtherFiles(string bundleId, FileInfo file, DumpMetainfo dumpInfo) {
			if (settings.Value.IncludeOtherFilesInReport) {
				var dir = file.Directory;
				foreach (var siblingFile in dir.EnumerateFiles()) {
					if (siblingFile.FullName == file.FullName) continue; // don't add actual dump file twice
					if (siblingFile.Name.EndsWith(".dmp", StringComparison.OrdinalIgnoreCase)) continue; // don't include other dumps from same dir
					if (siblingFile.Name.EndsWith(".core.gz", StringComparison.OrdinalIgnoreCase)) continue; // don't include other dumps from same dir
					await dumpRepo.AddFileCopy(dumpInfo.Id, siblingFile, SDFileType.SiblingFile);
				}
			}
		}

		private void ScheduleDownload(string bundleId, string url, string filename) {
			var outgoingMessageTracer = dynatraceSdk.TraceOutgoingMessage(messagingSystemInfo);
			outgoingMessageTracer.Trace(() => {
				string jobId = Hangfire.BackgroundJob.Enqueue<SuperDumpRepository>(repo => DownloadAndScheduleProcessFile(bundleId, url, filename, outgoingMessageTracer.GetDynatraceByteTag()));
				outgoingMessageTracer.SetVendorMessageId(jobId);
			});
		}

		[Hangfire.Queue("download", Order = 1)]
		public void DownloadAndScheduleProcessFile(string bundleId, string url, string filename, byte[] dynatraceTag = null) {
			var processTracer = dynatraceSdk.TraceIncomingMessageProcess(messagingSystemInfo);
			processTracer.SetDynatraceByteTag(dynatraceTag);
			processTracer.Trace(() => 
				AsyncHelper.RunSync(() => DownloadAndScheduleProcessFileAsync(bundleId, url, filename))
			);
		}

		public async Task DownloadAndScheduleProcessFileAsync(string bundleId, string url, string filename) {
			bundleRepo.SetBundleStatus(bundleId, BundleStatus.Downloading);
			try {
				using (TempDirectoryHandle tempDir = await downloadService.Download(bundleId, url, filename)) {
					if (settings.Value.DuplicationDetectionEnabled && !SetHashAndCheckIfDuplicated(bundleId, new FileInfo(Path.Combine(tempDir.Dir.FullName, filename)))) {
						// duplication detected
						return;
					}

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

		private bool SetHashAndCheckIfDuplicated(string bundleId, FileInfo archiveFile) {
			if (!archiveFile.Exists) {
				Console.WriteLine($"Unable to find file {archiveFile.FullName}! Aborting analysis.");
				return false;
			}
			string md5 = Utility.Md5ForFile(archiveFile);
			var duplicates = bundleRepo.GetAll().Where(b => b.Status != BundleStatus.Duplication && b.FileHash == md5);
			if (duplicates.Any()) {
				string originalBundleId = duplicates.First().BundleId;
				Task.Run(() => identicalRepository.AddIdenticalRelationship(originalBundleId, bundleId));//TODO Task necessary?
				Console.WriteLine($"This bundle has already been analyzed. See bundle id {originalBundleId}.");
				bundleRepo.Get(bundleId).OriginalBundleId = originalBundleId;
				bundleRepo.SetBundleStatus(bundleId, BundleStatus.Duplication);
				return false;
			}
			bundleRepo.Get(bundleId).FileHash = md5;
			return true;
		}

		public void RerunAnalysis(DumpIdentifier id) {
			var dumpFilePath = dumpRepo.GetDumpFilePath(id);
			if (!File.Exists(dumpFilePath)) {
				throw new DumpNotFoundException($"id: {id}, path: {dumpFilePath}");
			}

			WipeAllExceptDump(id);

			dumpRepo.ResetDumpTyp(id);
			var dumpInfo = dumpRepo.Get(id);
			analysisService.ScheduleDumpAnalysis(dumpInfo);
		}
	}
}