using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SuperDumpService.Helpers;
using SuperDumpService.Models;
using SuperDumpService.Services;
using SuperDumpService.ViewModels;

namespace SuperDumpService.Controllers
{
	[Authorize(Policy = LdapCookieAuthenticationExtension.ViewerPolicy)]
	public class SimilarityController : Controller
    {
		private readonly SimilarityService similarityService;
		private readonly DumpRepository dumpRepository;
		private readonly BundleRepository bundleRepository;
		private readonly ILogger<SimilarityController> logger;

		public SimilarityController(SimilarityService similarityService, 
				DumpRepository dumpRepository, BundleRepository bundleRepository, ILoggerFactory loggerFactory) {
			this.similarityService = similarityService;
			this.dumpRepository = dumpRepository;
			this.bundleRepository = bundleRepository;
			this.logger = loggerFactory.CreateLogger<SimilarityController>();
		}

		[HttpGet]
		public async Task<IActionResult> CompareDumps(string bundleId1, string dumpId1, string bundleId2, string dumpId2) {
			try {
				var id1 = new DumpIdentifier(bundleId1, dumpId1);
				var id2 = new DumpIdentifier(bundleId2, dumpId2);

				var res1 = await similarityService.GetOrCreateMiniInfo(id1);
				var res2 = await similarityService.GetOrCreateMiniInfo(id2);

				if (res1 == null || res2 == null) {
					return View(new SimilarityModel($"could not compare dumps."));
				}
				logger.LogSimilarityEvent("CompareDumps", HttpContext, bundleId1, dumpId1, bundleId2, dumpId2);
				var similarity = CrashSimilarity.Calculate(res1, res2);
				return View(new SimilarityModel(new DumpIdentifier(bundleId1, dumpId2), new DumpIdentifier(bundleId2, dumpId2), similarity));
			} catch (Exception e) {
				return View(new SimilarityModel($"exception while comparing: {e.ToString()}"));
			}
		}
	}
}