using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SuperDumpService.Models;
using SuperDump.Models;
using SuperDumpService.ViewModels;
using System.IO;
using SuperDumpService.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.Net.Mime;
using System.Text;
using SuperDumpService.Services;
using System.Linq;

namespace SuperDumpService.Controllers {
	public class HomeController : Controller {
		private IHostingEnvironment environment;
		public ISuperDumpRepository superDumpRepo { get; set; }
		public BundleRepository bundleRepo { get; private set; }
		public DumpRepository dumpRepo { get; private set; }

		public HomeController(IHostingEnvironment environment, ISuperDumpRepository superDumpRepo, BundleRepository bundleRepo, DumpRepository dumpRepo) {
			this.environment = environment;
			this.superDumpRepo = superDumpRepo;
			this.bundleRepo = bundleRepo;
			this.dumpRepo = dumpRepo;
			Console.WriteLine(Environment.CurrentDirectory);
			PathHelper.PrepareDirectories();
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
		public IActionResult Create(DumpBundle bundle) {
			PathHelper.PrepareDirectories();

			if (ModelState.IsValid) {
				System.Diagnostics.Debug.WriteLine(bundle.Url);

				try {
					string filename = bundle.UrlFilename;
					if (Utility.ValidateUrl(bundle.Url, ref filename)) {
						bundle.UrlFilename = filename;
						Utility.ScheduleBundleAnalysis(PathHelper.GetWorkingDir(), bundle);
						// return list of file paths from zip
						return RedirectToAction("BundleCreated", "Home", new { bundleId = bundle.Id });
					} else {
						return BadRequest("Provided URI is invalid or cannot be reached.");
					}
				} catch {
					superDumpRepo.DeleteBundle(bundle.Id);
					return BadRequest("Error while processing. SuperDump was not able to process it.");
				}
			} else {
				return View();
			}
		}

		public IActionResult BundleCreated(string bundleId) {
			if (superDumpRepo.ContainsBundle(bundleId)) {
				return View(new BundleViewModel(bundleRepo.Get(bundleId), dumpRepo.Get(bundleId)));
			}
			return null; // View(new BundleViewModel(BundleRepo.Create(bundleId)));
		}

		[HttpPost]
		public async Task<IActionResult> Upload(IFormFile file, string jiraIssue, string friendlyName) {
			if (ModelState.IsValid) {
				PathHelper.PrepareDirectories();
				if (file.Length > 0) {
					int i = 0;
					string filePath = Path.Combine(PathHelper.GetUploadsDir(), file.FileName);
					while (System.IO.File.Exists(filePath)) {
						filePath = Path.Combine(PathHelper.GetUploadsDir(),
							Path.GetFileNameWithoutExtension(file.FileName)
								+ "_" + i
								+ Path.GetExtension(filePath));

						i++;
					}
					using (var fileStream = new FileStream(filePath, FileMode.Create)) {
						await file.CopyToAsync(fileStream);
					}
					var bundle = new DumpBundle { Id = superDumpRepo.CreateUniqueBundleId(), Url = filePath, Path = filePath, JiraIssue = jiraIssue, FriendlyName = friendlyName };
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

			// get dump result from repository
			DumpAnalysisItem item = superDumpRepo.GetDump(bundleId, dumpId);
			if (item == null) {
				return View(null);
			}

			SDResult res;
			try {
				res = superDumpRepo.GetResult(bundleId, dumpId);
			} catch (Exception e) {
				res = null;
				Console.WriteLine(e.Message);
			}
			// TODO : render razor template with report data
			return View(new ReportViewModel(bundleId, dumpId, item.JiraIssue,
				item.FriendlyName, item.Path, item.TimeStamp, res,
				item.HasAnalysisFailed, item.AnalysisError, item.Files));
		}

		public IActionResult UploadError() {
			return View();
		}

		public IActionResult Error() {
			return View();
		}

		public IActionResult DownloadFile(string bundleId, string dumpId, string filename) {
			var file = superDumpRepo.GetReportFile(bundleId, dumpId, filename);
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
			return File(fileBytes, MediaTypeNames.Application.Octet, file.Name);
		}

		[HttpPost]
		public IActionResult Rerun(string bundleId, string dumpId) {
			// delete all files except dump
			superDumpRepo.WipeAllExceptDump(bundleId, dumpId);

			Utility.RerunAnalysis(bundleId, dumpId);
			return View(new ReportViewModel(bundleId, dumpId));
		}
	}
}
