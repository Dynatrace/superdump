using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SuperDumpService.Models;
using System.IO;
using SuperDumpService.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.Text;
using System.Linq;
using SuperDumpService.Services;
using SuperDump.Models;
using SuperDumpService.ViewModels;
using System.Collections.Generic;

namespace SuperDumpService.Controllers {
	public class HomeController : Controller {
		private IHostingEnvironment environment;
		public SuperDumpRepository superDumpRepo;
		public BundleRepository bundleRepo;
		public DumpRepository dumpRepo;
		public DumpStorageFilebased dumpStorage;
		private readonly PathHelper pathHelper;

		public HomeController(IHostingEnvironment environment, SuperDumpRepository superDumpRepo, BundleRepository bundleRepo, DumpRepository dumpRepo, DumpStorageFilebased dumpStorage, PathHelper pathHelper) {
			this.environment = environment;
			this.superDumpRepo = superDumpRepo;
			this.bundleRepo = bundleRepo;
			this.dumpRepo = dumpRepo;
			this.dumpStorage = dumpStorage;
			this.pathHelper = pathHelper;
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
					string bundleId = superDumpRepo.ProcessInputfile(filename, input);

					// return list of file paths from zip
					return RedirectToAction("BundleCreated", "Home", new { bundleId = bundleId });
				} else {
					return BadRequest("Provided URI is invalid or cannot be reached.");
				}
			} else {
				return View();
			}
		}

		public IActionResult BundleCreated(string bundleId) {
			if (bundleRepo.ContainsBundle(bundleId)) {
				return View(new BundleViewModel(bundleRepo.Get(bundleId), dumpRepo.Get(bundleId)));
			}
			throw new NotImplementedException("TODO better exception message");
		}

		[HttpPost]
		public async Task<IActionResult> Upload(IFormFile file, string jiraIssue, string friendlyName) {
			if (ModelState.IsValid) {
				pathHelper.PrepareDirectories();
				if (file.Length > 0) {
					int i = 0;
					string filePath = Path.Combine(pathHelper.GetUploadsDir(), file.FileName);
					while (System.IO.File.Exists(filePath)) {
						filePath = Path.Combine(pathHelper.GetUploadsDir(),
							Path.GetFileNameWithoutExtension(file.FileName)
								+ "_" + i
								+ Path.GetExtension(filePath));

						i++;
					}
					using (var fileStream = new FileStream(filePath, FileMode.Create)) {
						await file.CopyToAsync(fileStream);
					}
					var bundle = new DumpAnalysisInput { Url = filePath, JiraIssue = jiraIssue, FriendlyName = friendlyName };
					return Create(bundle);
				}
				return View("UploadError", new Error("No filename was provided.", ""));
			} else {
				return View("UploadError", new Error("Invalid model", "Invalid model"));
			}
		}

		public IActionResult Overview() {
			return View(bundleRepo.GetAll().Select(r => new BundleViewModel(r, dumpRepo.Get(r.BundleId))));
		}

		public IActionResult GetReport() {
			ViewData["Message"] = "Get Report";
			return View();
		}

		[HttpGet(Name = "Report")]
		public IActionResult Report(string bundleId, string dumpId) {
			ViewData["Message"] = "Get Report";

			var bundleInfo = superDumpRepo.GetBundle(bundleId);
			if (bundleInfo == null) {
				return View(null);
			}

			var dumpInfo = superDumpRepo.GetDump(bundleId, dumpId);
			if (dumpInfo == null) {
				return View(null);
			}


			SDResult res = superDumpRepo.GetResult(bundleId, dumpId);

			return View(new ReportViewModel(bundleId, dumpId) {
				BundleFileName = bundleInfo.BundleFileName,
				DumpFileName = dumpInfo.DumpFileName,
				Result = res,
				CustomProperties = Utility.Sanitize(bundleInfo.CustomProperties),
				HasAnalysisFailed = dumpInfo.Status == DumpStatus.Failed,
				TimeStamp = dumpInfo.Created,
				Files = dumpRepo.GetFileNames(bundleId, dumpId),
				AnalysisError = dumpInfo.ErrorMessage,
				ThreadTags = res != null ? res.GetThreadTags() : new HashSet<SDTag>(),
				PointerSize = res == null ? 8 : (res.SystemContext.ProcessArchitecture == "X86" ? 8 : 12)
			});
		}

		public IActionResult UploadError() {
			return View();
		}

		public IActionResult Error() {
			return View();
		}

		public IActionResult DownloadFile(string bundleId, string dumpId, string filename) {
			var file = dumpStorage.GetFile(bundleId, dumpId, filename);
			if (file == null) throw new ArgumentException("could not find file");
			if (file.Extension == ".txt"
				|| file.Extension == ".log"
				|| file.Extension == ".json") {
				var sb = new StringBuilder();
				using (var stream = new StreamReader(System.IO.File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))) {
					string s = string.Empty;
					while ((s = stream.ReadLine()) != null) {
						sb.AppendLine(s);
					}
				}
				return Content(sb.ToString());
			}
			byte[] fileBytes = System.IO.File.ReadAllBytes(file.FullName);
			return File(fileBytes, "application/octet-stream", file.Name);
		}

		[HttpPost]
		public IActionResult Rerun(string bundleId, string dumpId) {
			superDumpRepo.RerunAnalysis(bundleId, dumpId);
			return View(new ReportViewModel(bundleId, dumpId));
		}
	}
}
