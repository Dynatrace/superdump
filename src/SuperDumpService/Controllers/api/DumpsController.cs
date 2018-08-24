using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using SuperDumpService.Models;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using SuperDumpService.Services;
using SuperDumpService.Helpers;
using SuperDump.Models;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace SuperDumpService.Controllers.Api {
	[Route("api/[controller]")]
	public class DumpsController : Controller {
		public SuperDumpRepository superDumpRepo;
		public BundleRepository bundleRepo;
		public DumpRepository dumpRepo;
		private readonly ILogger<DumpsController> logger;

		public DumpsController(SuperDumpRepository superDumpRepo, BundleRepository bundleRepo, DumpRepository dumpRepo, ILoggerFactory loggerFactory) {
			this.superDumpRepo = superDumpRepo;
			this.bundleRepo = bundleRepo;
			this.dumpRepo = dumpRepo;
			logger = loggerFactory.CreateLogger<DumpsController>();
		}

		/// <summary>
		/// Returns analysis data for requested bundle
		/// </summary>
		/// <param name="bundleId">ID of the requested bundle or dump</param>
		/// <returns>JSON array, if id was a bundle id, or a single JSON entry for a dump id</returns>
		/// <response code="200">Returned JSON data for all dumps in bundle</response>
		/// <response code="404">If result is not ready, or dump does not exist</response>
		[Authorize(Policy = LdapCookieAuthenticationExtension.UserPolicy)]
		[HttpGet("{bundleId}", Name = "dumps")]
		[ProducesResponseType(typeof(List<SDResult>), 200)]
		[ProducesResponseType(typeof(string), 404)]
		public async Task<IActionResult> Get(string bundleId) {
			// check if it is a bundle 
			var bundleInfo = superDumpRepo.GetBundle(bundleId);
			if (bundleInfo == null) {
				logger.LogNotFound("Api: Bundle not found", HttpContext, "BundleId", bundleId);
				return NotFound("Resource not found");
			}
			logger.LogBundleAccess("Api get Bundle", HttpContext, bundleInfo);
			var resultList = new List<SDResult>();
			foreach (var dumpInfo in dumpRepo.Get(bundleId)) {
				resultList.Add(await superDumpRepo.GetResult(bundleId, dumpInfo.DumpId));
			}
			return Content(JsonConvert.SerializeObject(resultList, Formatting.Indented, new JsonSerializerSettings {
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			}), "application/json");
		}

		/// <summary>
		/// Creates a new DumpBundle object on the server
		/// </summary>
		/// <param name="input">DumpBundle that's gonna be stored on server</param>
		/// <returns>Created resource</returns>
		/// <response code="400">If url was invalid, or SuperDump had an error when processing</response>
		/// <response code="201"></response>
		[HttpPost]
		[ProducesResponseType(typeof(void), 201)]
		[ProducesResponseType(typeof(string), 400)]
		public IActionResult Post([FromBody]DumpAnalysisInput input) {
			if (ModelState.IsValid) {

				string filename = input.UrlFilename;
				//validate URL
				if (Utility.ValidateUrl(input.Url, ref filename)) {
					if (filename == null && Utility.IsLocalFile(input.Url)) {
						filename = Path.GetFileName(input.Url);
					}
					string bundleId = superDumpRepo.ProcessInputfile(filename, input);
					if (bundleId != null) {
						logger.LogFileUpload("Api Upload", HttpContext, bundleId, input.CustomProperties, input.Url);
						return CreatedAtAction(nameof(HomeController.BundleCreated), "Home", new { bundleId = bundleId }, null);
					} else {
						// in case the input was just symbol files, we don't get a bundleid.
						// TODO
						throw new NotImplementedException();
					}
				} else {
					logger.LogNotFound("Api Upload: File not found", HttpContext, "Url", input.Url);
					return BadRequest("Invalid request, resource identifier is not valid or cannot be reached.");
				}
			} else {
				return BadRequest("Invalid request, check if value was set.");
			}
		}
	}
}
