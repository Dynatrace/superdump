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
			throw new NotImplementedException($"bundleid '{bundleId}' does not exist in repository");
		}

		[HttpPost]
		public async Task<IActionResult> Upload(IFormFile file, string refurl, string note) {
			if (ModelState.IsValid) {
				pathHelper.PrepareDirectories();
				if (file.Length > 0) {
					var tempDir = new DirectoryInfo(pathHelper.GetTempDir());
					tempDir.Create();
					var filePath = new FileInfo(Path.Combine(tempDir.FullName, file.FileName));
					using (var fileStream = new FileStream(filePath.FullName, FileMode.Create)) {
						await file.CopyToAsync(fileStream);
					}
					var bundle = new DumpAnalysisInput(filePath.FullName, new Tuple<string, string>("ref", refurl), new Tuple<string, string>("note", note));
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

		[HttpGet(Name = "Interactive")]
		public IActionResult Interactive(string bundleId, string dumpId, string cmd) {
			return View(new InteractiveViewModel() { BundleId = bundleId, DumpId = dumpId, DumpInfo = dumpRepo.Get(bundleId, dumpId), Command = cmd });
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

			SDResult res = superDumpRepo.GetResult(bundleId, dumpId, out string error);

			return base.View(new ReportViewModel(bundleId, dumpId) {
				BundleFileName = bundleInfo.BundleFileName,
				DumpFileName = dumpInfo.DumpFileName,
				Result = res,
				CustomProperties = Utility.Sanitize(bundleInfo.CustomProperties),
				HasAnalysisFailed = dumpInfo.Status == DumpStatus.Failed,
				TimeStamp = dumpInfo.Created,
				Files = dumpRepo.GetFileNames(bundleId, dumpId),
				AnalysisError = dumpInfo.ErrorMessage,
				ThreadTags = res != null ? res.GetThreadTags() : new HashSet<SDTag>(),
				PointerSize = res == null ? 8 : (res.SystemContext.ProcessArchitecture == "X86" ? 8 : 12),
				CustomTextResult = ReadCustomTextResult(dumpInfo),
				SDResultReadError = error,
				DumpType = dumpInfo.DumpType
			});
		}

		private string ReadCustomTextResult(DumpMetainfo dumpInfo) {
			SDFileEntry customResultFile = dumpInfo.Files.FirstOrDefault(x => x.Type == SDFileType.CustomTextResult);
			if (customResultFile == null) return null;
			FileInfo file = dumpStorage.GetFile(dumpInfo.BundleId, dumpInfo.DumpId, customResultFile.FileName);
			if (file == null || !file.Exists) return null;
			return System.IO.File.ReadAllText(file.FullName);
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
				return Content(System.IO.File.ReadAllText(file.FullName));
			}
			return File(System.IO.File.ReadAllBytes(file.FullName), "application/octet-stream", file.Name);
		}

		[HttpPost]
		public IActionResult Rerun(string bundleId, string dumpId) {
			superDumpRepo.RerunAnalysis(bundleId, dumpId);
			return View(new ReportViewModel(bundleId, dumpId));
		}
	}
}
