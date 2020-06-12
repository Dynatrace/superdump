using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using X.PagedList;
using SuperDump.Models;
using SuperDumpService.Helpers;
using SuperDumpService.Models;
using SuperDumpService.Services;
using SuperDumpService.ViewModels;

namespace SuperDumpService.Controllers {
	[AutoValidateAntiforgeryToken]
	[Authorize(Policy = LdapCookieAuthenticationExtension.ViewerPolicy)]
	public class HomeController : Controller {
		private readonly SuperDumpRepository superDumpRepo;
		private readonly BundleRepository bundleRepo;
		private readonly DumpRepository dumpRepo;
		private readonly IDumpStorage dumpStorage;
		private readonly SuperDumpSettings settings;
		private readonly PathHelper pathHelper;
		private readonly RelationshipRepository relationshipRepo;
		private readonly SimilarityService similarityService;
		private readonly ILogger<HomeController> logger;
		private readonly IAuthorizationHelper authorizationHelper;
		private readonly JiraIssueRepository jiraIssueRepository;
		private readonly SearchService searchService;
		private readonly DownloadService downloadService;

		public HomeController( 
				SuperDumpRepository superDumpRepo, 
				BundleRepository bundleRepo, 
				DumpRepository dumpRepo,
				IDumpStorage dumpStorage,
				IOptions<SuperDumpSettings> settings,
				PathHelper pathHelper,
				RelationshipRepository relationshipRepo,
				SimilarityService similarityService,
				ElasticSearchService elasticService,
				ILoggerFactory loggerFactory,
				IAuthorizationHelper authorizationHelper,
				JiraIssueRepository jiraIssueRepository,
				SearchService searchService,
				DownloadService downloadService) {
			this.superDumpRepo = superDumpRepo;
			this.bundleRepo = bundleRepo;
			this.dumpRepo = dumpRepo;
			this.dumpStorage = dumpStorage;
			this.settings = settings.Value;
			this.pathHelper = pathHelper;
			this.relationshipRepo = relationshipRepo;
			this.similarityService = similarityService;
			logger = loggerFactory.CreateLogger<HomeController>();
			this.authorizationHelper = authorizationHelper;
			this.jiraIssueRepository = jiraIssueRepository;
			this.searchService = searchService;
			this.downloadService = downloadService;
		}

		public IActionResult Index() {
			return RedirectToAction("Create");
		}

		public IActionResult About() {
			ViewData["Message"] = "SuperDump";
			return View();
		}

		[HttpGet]
		public IActionResult Create() {
			ViewData["Message"] = "New Analysis";
			return View();
		}

		[HttpPost]
		public IActionResult Create(DumpAnalysisInput input) {
			pathHelper.PrepareDirectories();

			if (ModelState.IsValid) {
				System.Diagnostics.Debug.WriteLine(input.Url);

				string filename = input.UrlFilename;
				if (Utility.ValidateUrl(input.Url, ref filename)) {
					if (filename == null && Utility.IsLocalFile(input.Url)) {
						filename = Path.GetFileName(input.Url);
					}
					string bundleId = superDumpRepo.ProcessWebInputfile(filename, input);
					logger.LogFileUpload("Upload", HttpContext, bundleId, input.CustomProperties, input.Url);
					// return list of file paths from zip
					return RedirectToAction("BundleCreated", "Home", new { bundleId = bundleId });
				} else {
					logger.LogNotFound("Upload", HttpContext, "Url", input.Url);
					return BadRequest("Provided URI is invalid or cannot be reached.");
				}
			} else {
				return View();
			}
		}

		public async Task<IActionResult> BundleCreated(string bundleId) {
			if (bundleRepo.ContainsBundle(bundleId)) {
				return View("BundleCreated", new BundleViewModel(bundleRepo.Get(bundleId), await GetDumpListViewModels(bundleId)));
			}
			throw new Exception($"bundleid '{bundleId}' does not exist in repository");
		}

		private async Task<IEnumerable<DumpViewModel>> GetDumpListViewModels(string bundleId) {
			var bundleInfo = bundleRepo.Get(bundleId);
			if (relationshipRepo.IsPopulated) {
				return await Task.WhenAll(dumpRepo.Get(bundleId).Select(async x => 
					new DumpViewModel(x, new BundleViewModel(bundleInfo), 
					new Similarities(await similarityService.GetSimilarities(x.Id)), 
					new RetentionViewModel(x, dumpRepo.IsPrimaryDumpAvailable(x.Id), 
						TimeSpan.FromDays(settings.WarnBeforeDeletionInDays),
						settings.UseJiraIntegration && jiraIssueRepository.IsPopulated && jiraIssueRepository.HasBundleOpenIssues(bundleId)))));
			}
			return dumpRepo.Get(bundleId).Select(x => new DumpViewModel(x, new BundleViewModel(bundleInfo)));
		}

		[HttpPost]
		[RequestSizeLimit(4294967295)] //set max allowed request content length to 4GB - 1byte, the configuration in the web.config file does not work in .net core 3.0 preview 6
		public async Task<IActionResult> Upload(IFormFile file, string refurl, string note) {
			if (ModelState.IsValid) {
				pathHelper.PrepareDirectories();
				if (file.Length > 0) {
					var tempFileHandle = await downloadService.Download(file.OpenReadStream(), file.FileName);
					string bundleId = superDumpRepo.ProcessLocalInputfile(file.FileName, tempFileHandle, new Dictionary<string, string> { { "ref", refurl }, { "note", note } });
					return RedirectToAction("BundleCreated", "Home", new { bundleId = bundleId });
				}
				return View("UploadError", new Error("No filename was provided.", ""));
			} else {
				return View("UploadError", new Error("Invalid model", "Invalid model"));
			}
		}

		public async Task<IActionResult> Dumps(
				int page = 1,
				int pagesize = 50,
				string searchFilter = null,
				bool includeEmptyBundles = false,
				string elasticSearchFilter = null,
				string duplBundleId = null,
				string duplDumpId = null
			) {
			logger.LogDefault("Dumps", HttpContext);

			IOrderedEnumerable<DumpViewModel> dumpViewModels = null;

			if (!string.IsNullOrEmpty(duplBundleId) && !string.IsNullOrEmpty(duplDumpId)) {
				// find duplicates of given bundleId+dumpId
				var id = DumpIdentifier.Create(duplBundleId, duplDumpId);
				logger.LogSearch("Duplicates", HttpContext, id.ToString());
				dumpViewModels = await searchService.SearchDuplicates(id);
				ViewData["duplBundleId"] = duplBundleId;
				ViewData["duplDumpId"] = duplDumpId;
			} else if (!string.IsNullOrEmpty(elasticSearchFilter)) {
				// run elasticsearch query
				logger.LogSearch("Search", HttpContext, elasticSearchFilter);
				dumpViewModels = await searchService.SearchByElasticFilter(elasticSearchFilter);
				ViewData["elasticSearchFilter"] = elasticSearchFilter;
			} else {
				// do plain search, or show all of searchFilter is empty
				logger.LogSearch("Search", HttpContext, searchFilter);
				dumpViewModels = await searchService.SearchBySimpleFilter(searchFilter);
				ViewData["searchFilter"] = searchFilter;
			}

			return View(new DumpsViewModel {
				Filtered = dumpViewModels,
				Paged = dumpViewModels.ToPagedList(page, pagesize),
				KibanaUrl = KibanaUrl(),
				IsPopulated = bundleRepo.IsPopulated,
				IsRelationshipsPopulated = relationshipRepo.IsPopulated || !settings.SimilarityDetectionEnabled,
				IsJiraIssuesPopulated = jiraIssueRepository.IsPopulated || !settings.UseJiraIntegration,
				UseAutomaticDumpDeletion = settings.IsDumpRetentionEnabled()
			});
		}

		[HttpGet(Name = "Elastic")]
		public IActionResult Elastic() {
			logger.LogDefault("ElasticSearch", HttpContext);
			return Redirect(KibanaUrl());
		}

		private string KibanaUrl() {
			string portlessUrl = settings.ElasticSearchHost;
			if (portlessUrl.Contains(':')) {
				int colon = portlessUrl.LastIndexOf(':');
				portlessUrl = portlessUrl.Substring(0, colon);
			}
			return portlessUrl + ":5601";
		}

		public IActionResult GetReport() {
			ViewData["Message"] = "Get Report";
			return View();
		}

		[Authorize(Policy = LdapCookieAuthenticationExtension.UserPolicy)]
		[HttpGet(Name = "Interactive")]
		public IActionResult Interactive(string bundleId, string dumpId, string cmd) {
			var bundleInfo = superDumpRepo.GetBundle(bundleId);
			if (bundleInfo == null) {
				logger.LogNotFound("Interactive Mode: Bundle not found", HttpContext, "BundleId", bundleId);
				return View(null);
			}

			logger.LogDumpAccess("Start Interactive Mode", HttpContext, bundleInfo, dumpId);
			var id = DumpIdentifier.Create(bundleId, dumpId);
			return View(new InteractiveViewModel() { Id = id, DumpInfo = dumpRepo.Get(id), Command = cmd });
		}

		/// <summary>
		/// Generic view that shows either the dump report or the bundle overview. e.g.
		/// Home/Dump?id=nlu7029:irb4329      // show dump report
		/// Home/Dump?id=nlu7029              // show bundle report
		/// </summary>
		/// <param name="id"></param>
		[HttpGet(Name = "Dump")]
		public async Task<IActionResult> Dump(string id) {
			var dumpIdentifier = DumpIdentifier.Parse(id);
			if (dumpIdentifier != null) {
				// valid identifier, show dump
				return await Report(dumpIdentifier.BundleId, dumpIdentifier.DumpId);
			} else {
				// invalid identifier. assume only bundleid. try to show bundle info
				return await BundleCreated(id);
			}
		}

		[HttpGet(Name = "Report")]
		public async Task<IActionResult> Report(string bundleId, string dumpId) {
			ViewData["Message"] = "Get Report";
			var id = DumpIdentifier.Create(bundleId, dumpId);

			var bundleInfo = superDumpRepo.GetBundle(bundleId);
			if (bundleInfo == null) {
				logger.LogNotFound("Report: Bundle not found", HttpContext, "BundleId", bundleId);
				return View(null);
			}

			var dumpInfo = superDumpRepo.GetDump(id);
			if (dumpInfo == null) {
				logger.LogNotFound("Report: Dump not found", HttpContext, "Id", id.ToString());
				return View(null);
			}

			logger.LogDumpAccess("Report", HttpContext, bundleInfo, dumpId);

			string sdReadError = string.Empty;
			SDResult res = null;
			try {
				res = await superDumpRepo.GetResultAndThrow(id);
			} catch (Exception e) {
				sdReadError = e.ToString();
			}

			// don't add relationships when the repo is not ready yet. it might take some time with large amounts.
			IEnumerable<KeyValuePair<DumpMetainfo, double>> similarDumps =
				!relationshipRepo.IsPopulated ? Enumerable.Empty<KeyValuePair<DumpMetainfo, double>>() :
				(await relationshipRepo.GetRelationShips(DumpIdentifier.Create(bundleId, dumpId)))
					.Select(x => new KeyValuePair<DumpMetainfo, double>(dumpRepo.Get(x.Key), x.Value)).Where(dump => dump.Key != null);

			return base.View("Report", new ReportViewModel(id) {
				BundleFileName = bundleInfo.BundleFileName,
				DumpFileName = dumpInfo.DumpFileName,
				Result = res,
				CustomProperties = Utility.Sanitize(bundleInfo.CustomProperties),
				TimeStamp = dumpInfo.Created,
				Files = dumpRepo.GetFileNames(id),
				AnalysisError = dumpInfo.ErrorMessage,
				ThreadTags = res != null ? res.GetThreadTags() : new HashSet<SDTag>(),
				PointerSize = res == null ? 8 : (res.SystemContext?.ProcessArchitecture == "X86" ? 8 : 12),
				CustomTextResult = await ReadCustomTextResult(dumpInfo),
				SDResultReadError = sdReadError,
				DumpType = dumpInfo.DumpType,
				RepositoryUrl = settings.RepositoryUrl,
				InteractiveGdbHost = settings.InteractiveGdbHost,
				SimilarityDetectionEnabled = settings.SimilarityDetectionEnabled,
				Similarities = similarDumps,
				MainBundleJiraIssues = !settings.UseJiraIntegration || !jiraIssueRepository.IsPopulated ? Enumerable.Empty<JiraIssueModel>() : await jiraIssueRepository.GetAllIssuesByBundleIdWithoutWait(bundleId),
				SimilarDumpIssues = !settings.UseJiraIntegration || !jiraIssueRepository.IsPopulated ? new Dictionary<string, IEnumerable<JiraIssueModel>>() : await jiraIssueRepository.GetAllIssuesByBundleIdsWithoutWait(similarDumps.Select(dump => dump.Key.BundleId)),
				UseJiraIntegration = settings.UseJiraIntegration,
				DumpStatus = dumpInfo.Status,
				IsRelationshipsPopulated = relationshipRepo.IsPopulated || !settings.SimilarityDetectionEnabled,
				IsJiraIssuesPopulated = jiraIssueRepository.IsPopulated || !settings.UseJiraIntegration,
				UseAutomaticDumpDeletion = settings.IsDumpRetentionEnabled(),
                DumpRetentionExtensionDays = settings.DumpRetentionExtensionDays,
				RetentionViewModel = new RetentionViewModel(
					dumpInfo,
					dumpRepo.IsPrimaryDumpAvailable(id),
					TimeSpan.FromDays(settings.WarnBeforeDeletionInDays),
					settings.UseJiraIntegration && jiraIssueRepository.IsPopulated && jiraIssueRepository.HasBundleOpenIssues(bundleId))
			});
		}

		private async Task<string> ReadCustomTextResult(DumpMetainfo dumpInfo) {
			SDFileEntry customResultFile = dumpInfo.Files.FirstOrDefault(x => x.Type == SDFileType.CustomTextResult);
			if (customResultFile == null) return null;
			FileInfo file = dumpStorage.GetFile(dumpInfo.Id, customResultFile.FileName);
			if (file == null || !file.Exists) return null;
			return await System.IO.File.ReadAllTextAsync(file.FullName);
		}

		public IActionResult UploadError() {
			return View();
		}

		public IActionResult Error() {
			return View();
		}

		public IActionResult DownloadFile(string bundleId, string dumpId, string filename) {
			if (!(authorizationHelper.CheckPolicy(HttpContext.User, LdapCookieAuthenticationExtension.UserPolicy) ||
				settings.LdapAuthenticationSettings.ViewerDownloadableFiles.Any(f => f == filename) &&
				authorizationHelper.CheckPolicy(HttpContext.User, LdapCookieAuthenticationExtension.ViewerPolicy))) {
				return Forbid();
			}

			var bundleInfo = superDumpRepo.GetBundle(bundleId);
			if (bundleInfo == null) {
				logger.LogNotFound("DownloadFile: Bundle not found", HttpContext, "BundleId", bundleId);
				return View(null);
			}
			var file = dumpStorage.GetFile(DumpIdentifier.Create(bundleId, dumpId), filename);
			if (file == null) {
				logger.LogNotFound("DownloadFile: File not found", HttpContext, "Filename", filename);
				throw new ArgumentException("could not find file");
			}
			logger.LogFileAccess("DownloadFile", HttpContext, bundleInfo, dumpId, filename);
			if (file.Extension == ".txt"
				|| file.Extension == ".log"
				|| file.Extension == ".json") {
				return ContentWithFilename(System.IO.File.ReadAllText(file.FullName), file.Name);
			}
			return File(System.IO.File.OpenRead(file.FullName), "application/octet-stream", file.Name);
		}

		/// <summary>
		/// Adds Filename to Content-Disposition Headers, so that "Save As..." in the browser uses the correct file name.
		/// When normally requesting this, the content is direclty shown in the browser.
		/// </summary>
		private IActionResult ContentWithFilename(string content, string filename) {
			var cd = new ContentDisposition {
				FileName = filename,
				Inline = true  // false = prompt the user for downloading;  true = browser to try to show the file inline
			};
			Response.Headers.Add("Content-Disposition", cd.ToString());
			Response.Headers.Add("X-Content-Type-Options", "nosniff");
			return Content(content);
		}

		[Authorize(Policy = LdapCookieAuthenticationExtension.UserPolicy)]
		[HttpPost]
		public IActionResult Rerun(string bundleId, string dumpId) {
			var bundleInfo = superDumpRepo.GetBundle(bundleId);
			if (bundleInfo == null) {
				logger.LogNotFound("Rerun: Bundle not found", HttpContext, "BundleId", bundleId);
				return View(null);
			}
			logger.LogDumpAccess("Rerun", HttpContext, bundleInfo, dumpId);
			var id = DumpIdentifier.Create(bundleId, dumpId);
			superDumpRepo.RerunAnalysis(id);
			return View(new ReportViewModel(id));
		}

		[Authorize(Policy = LdapCookieAuthenticationExtension.UserPolicy)]
		[HttpPost]
		public IActionResult ExtendRetentionTime(string bundleId, string dumpId) {
			var bundleInfo = superDumpRepo.GetBundle(bundleId);
			if (bundleInfo == null) {
				logger.LogNotFound("ExtendRetentionTime: Bundle not found", HttpContext, "BundleId", bundleId);
				return View(null);
			}
			var id = DumpIdentifier.Create(bundleId, dumpId);
			var dumpInfo = superDumpRepo.GetDump(id);
			if (dumpInfo == null) {
				logger.LogNotFound("ExtendRetentionTime: Dump not found", HttpContext, "Id", id.ToString());
				return View(null);
			}

            var newPlannedDeletionDate = DateTime.Now + TimeSpan.FromDays(settings.DumpRetentionExtensionDays);
            if (dumpInfo.PlannedDeletionDate < newPlannedDeletionDate) {
                logger.LogDumpAccess("ExtendRetentionTime", HttpContext, bundleInfo, dumpId);
                dumpRepo.SetPlannedDeletionDate(id, newPlannedDeletionDate,
                    $"The Retention Time was extended to {settings.DumpRetentionExtensionDays} days by user {HttpContext.User.Identity.Name}");
            } else {
                logger.LogDumpAccess("ExtendRetentionTime: failed to extend dump retention time since the new one was shorter", HttpContext, bundleInfo, dumpId);
            }

			return RedirectToAction("Report", new { bundleId, dumpId });
		}
	}
}
