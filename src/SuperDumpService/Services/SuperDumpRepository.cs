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
using Microsoft.Extensions.Logging;

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

		private readonly ILogger<SuperDumpRepository> logger;

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
				IOneAgentSdk dynatraceSdk,
				ILoggerFactory loggerFactory) {
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

			this.logger = loggerFactory.CreateLogger<SuperDumpRepository>();
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

		public string ProcessWebInputfile(DumpAnalysisInput input) {
			string filename = input.UrlFilename;
			//validate URL
			if (Utility.ValidateUrl(input.Url, ref filename)) {
				if (filename == null && Utility.IsLocalFile(input.Url)) {
					filename = Path.GetFileName(input.Url);
				}
				return ProcessWebInputfile(filename, input);
			}
			return string.Empty;
		}

		/// <summary>
		/// create bundle, process the file
		/// </summary>
		public string ProcessWebInputfile(string filename, DumpAnalysisInput input) {
			var bundleInfo = bundleRepo.Create(filename, input.CustomProperties);
			ScheduleDownload(bundleInfo.BundleId, input.Url, filename); // indirectly calls ProcessFile()
			return bundleInfo.BundleId;
		}

		/// <summary>
		/// create bundle, process the file
		/// </summary>
		public string ProcessLocalInputfile(string filename, TempFileHandle tempFile, IDictionary<string, string> properties) {
			var bundleInfo = bundleRepo.Create(filename, properties);
			ScheduleProcessLocalFile(bundleInfo.BundleId, filename, tempFile);
			return bundleInfo.BundleId;
		}

		/// <summary>
		/// Unpacks the given file if it is an archive and adds pdb files to the symbol store.
		/// </summary>
		public async Task ProcessFile(string bundleId, FileInfo file) {
			if (file.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) { await ProcessArchive(bundleId, file, ArchiveType.Zip); return; }
			if (file.Name.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) && file.Name != "libs.tar.gz") { await ProcessArchive(bundleId, file, ArchiveType.TarGz); return; }
			if (file.Name.EndsWith(".tar", StringComparison.OrdinalIgnoreCase)) { await ProcessArchive(bundleId, file, ArchiveType.Tar); return; }
			if (file.Name.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase)) { ProcessSymbol(file); return; }
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

		private async Task ProcessArchive(string bundleId, FileInfo archiveFile, ArchiveType type) {
			DirectoryInfo dir = null;
			try {
				dir = unpackService.ExtractArchive(archiveFile, type);
			} catch (Exception e) {
				logger.LogArchiveUnpackException(bundleId, archiveFile, e);
			}
			if (dir != null) {
				await ProcessDirRecursive(bundleId, dir);
			}
		}

		private async Task IncludeOtherFiles(DirectoryInfo dir, DumpMetainfo dumpInfo, HashSet<string> foundPrimaryDumps) {
			if (settings.Value.IncludeOtherFilesInReport) {
				foreach (FileInfo siblingFile in dir.EnumerateFiles()) {
					if (UnpackService.IsSupportedArchive(siblingFile.Name)) { continue; }
					if (foundPrimaryDumps.Contains(siblingFile.Name)) { continue; }
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

		[Hangfire.Queue("download", Order = 1)]
		public void ScheduleProcessLocalFile(string bundleId, string filename, TempFileHandle tempFile, byte[] dynatraceTag = null) {
			var processTracer = dynatraceSdk.TraceIncomingMessageProcess(messagingSystemInfo);
			processTracer.SetDynatraceByteTag(dynatraceTag);
			processTracer.Trace(() =>
				AsyncHelper.RunSync(() => ProcessFileAsync(bundleId, filename, tempFile))
			);
			tempFile.Dispose(); // now it's safe to delete temp file
		}

		private void ScheduleProcessing(string bundleId, string url, string filename) {
			var outgoingMessageTracer = dynatraceSdk.TraceOutgoingMessage(messagingSystemInfo);
			outgoingMessageTracer.Trace(() => {
				string jobId = Hangfire.BackgroundJob.Enqueue<SuperDumpRepository>(repo => DownloadAndScheduleProcessFile(bundleId, url, filename, outgoingMessageTracer.GetDynatraceByteTag()));
				outgoingMessageTracer.SetVendorMessageId(jobId);
			});
		}

		public async Task DownloadAndScheduleProcessFileAsync(string bundleId, string url, string filename) {
			bundleRepo.SetBundleStatus(bundleId, BundleStatus.Downloading);
			try {
				using (TempFileHandle tempFile = await downloadService.Download(bundleId, url, filename)) {
					await ProcessFileAsync(bundleId, filename, tempFile);
				}
			} catch (Exception e) {
				bundleRepo.SetBundleStatus(bundleId, BundleStatus.Failed, e.ToString());
			}
		}

		private async Task ProcessFileAsync(string bundleId, string filename, TempFileHandle tempFile) {
			if (settings.Value.DuplicationDetectionEnabled && !SetHashAndCheckIfDuplicated(bundleId, tempFile.File)) {
				// duplication detected
				return;
			}

			bundleRepo.SetBundleStatus(bundleId, BundleStatus.Analyzing);
			try {
				await ProcessFile(bundleId, tempFile.File);
				IEnumerable<DumpMetainfo> dumps = await InitialAnalysis(bundleId, tempFile.File.Directory);
				analysisService.ScheduleDumpAnalysis(dumps);

				bundleRepo.SetBundleStatus(bundleId, BundleStatus.Finished);
			} catch (Exception e) {
				bundleRepo.SetBundleStatus(bundleId, BundleStatus.Failed, e.ToString());
			}
		}

		private async Task<IEnumerable<DumpMetainfo>> InitialAnalysis(string bundleId, DirectoryInfo directory) {
			var dumpMetainfos = new List<DumpMetainfo>();

			dumpMetainfos.AddRange(await analysisService.InitialAnalysis(bundleId, directory));
			var primaryDumps = dumpMetainfos.Select(dump => Path.GetFileName(dump.DumpFileName)).ToHashSet();
			foreach (var dump in dumpMetainfos) {
				await IncludeOtherFiles(directory, dump, primaryDumps);
			}

			foreach (DirectoryInfo childDirectory in directory.GetDirectories()) {
				dumpMetainfos.AddRange(await InitialAnalysis(bundleId, childDirectory));
			}
			return dumpMetainfos;
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