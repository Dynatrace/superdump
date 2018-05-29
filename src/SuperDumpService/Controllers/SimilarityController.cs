using System;
using Microsoft.AspNetCore.Mvc;
using SuperDumpService.Services;
using System.Threading.Tasks;
using SuperDumpService.Models;
using SuperDumpService.ViewModels;

namespace SuperDumpService.Controllers {
	public class SimilarityController : Controller {

		private readonly SimilarityService similarityService;
		private readonly DumpRepository dumpRepository;

		public SimilarityController(SimilarityService similarityService, DumpRepository dumpRepository) {
			this.similarityService = similarityService;
			this.dumpRepository = dumpRepository;
		}

		[HttpGet]
		public IActionResult Overview() {
			return View();
		}

		[HttpGet]
		public IActionResult CompareDumps(string bundleId1, string dumpId1, string bundleId2, string dumpId2) {
			try {
				var res1 = dumpRepository.GetResult(bundleId1, dumpId1, out string error1);
				var res2 = dumpRepository.GetResult(bundleId2, dumpId2, out string error2);

				if (res1 == null || res2 == null) {
					return View(new SimilarityModel($"could not compare dumps. {error1} {error2}"));
				}
				var similarity = CrashSimilarity.Calculate(res1, res2);
				return View(new SimilarityModel(new DumpIdentifier(bundleId1, dumpId2), new DumpIdentifier(bundleId2, dumpId2), similarity));
			} catch (Exception e) {
				return View(new SimilarityModel($"exception while comparing: {e.ToString()}"));
			}
		}

		[HttpPost]
		public IActionResult TriggerSimilarityAnalysis(string bundleId, string dumpId) {
			similarityService.ScheduleSimilarityAnalysis(new DumpIdentifier(bundleId, dumpId), true);
			return RedirectToAction("Report", "Home", new { bundleId = bundleId, dumpId = dumpId }); // View("/Home/Report", new ReportViewModel(bundleId, dumpId));
		}

		[HttpPost]
		public IActionResult TriggerSimilarityAnalysisForAllDumps(bool force) {
			similarityService.TriggerSimilarityAnalysisForAllDumps(force);
			return View("Overview");
		}
		
	}
}
