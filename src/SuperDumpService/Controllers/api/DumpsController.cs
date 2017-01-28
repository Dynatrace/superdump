using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using SuperDumpService.Models;
using SuperDumpService.Helpers;
using SuperDump.Models;
using Newtonsoft.Json;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace SuperDumpService.Controllers.Api {
	[Route("api/[controller]")]
	public class DumpsController : Controller {
		public IDumpRepository Dumps { get; set; }
		public DumpsController(IDumpRepository dumps) {
			this.Dumps = dumps;
		}

		/// <summary>
		/// Returns analysis data for requested bundle
		/// </summary>
		/// <param name="id">ID of the requested bundle or dump</param>
		/// <returns>JSON array, if id was a bundle id, or a single JSON entry for a dump id</returns>
		/// <response code="200">Returned JSON data for all dumps in bundle</response>
		/// <response code="404">If result is not ready, or dump does not exist</response>
		[HttpGet("{id}", Name = "dumps")]
		[ProducesResponseType(typeof(SDResult), 200)]
		[ProducesResponseType(typeof(string), 404)]
		public IActionResult Get(string id) {
			// check if it is a bundle 
			if (Dumps.ContainsBundle(id)) {
				DumpBundle bundle = Dumps.GetBundle(id);
				var resultList = new List<SDResult>();
				if (bundle != null) {
					foreach (var item in bundle.DumpItems.Values) {
						resultList.Add(Dumps.GetResult(bundle.Id, item.Id));
					}
					if (resultList.Count >= 0) {
						return Content(JsonConvert.SerializeObject(resultList, Formatting.Indented, new JsonSerializerSettings {
							ReferenceLoopHandling = ReferenceLoopHandling.Ignore
						}), "application/json");
					}
				} else {
					return NotFound("Resource not ready yet");
				}
			} else {
				//try to find a dump
				if (Dumps.ContainsDump(id)) {
					DumpAnalysisItem item = Dumps.GetDump(id);
					SDResult res = Dumps.GetResult(item.BundleId, id);
					if (item != null && res != null) {
						return Content(JsonConvert.SerializeObject(res, Formatting.Indented, new JsonSerializerSettings {
							ReferenceLoopHandling = ReferenceLoopHandling.Ignore
						}), "application/json");
					} else {
						return NotFound("Resource not ready yet");
					}
				} else {
					return NotFound("Resource not found");
				}
			}
			return NotFound("Resource not found");
		}

		/// <summary>
		/// Creates a new DumpBundle object on the server
		/// </summary>
		/// <param name="bundle">DumpBundle that's gonna be stored on server</param>
		/// <returns>Created resource</returns>
		/// <response code="400">If url was invalid, or SuperDump had an error when processing</response>
		/// <response code="201">Returns the newly created DumpAnalysisItem</response>
		[HttpPost]
		[ProducesResponseType(typeof(DumpBundle), 201)]
		[ProducesResponseType(typeof(string), 400)]
		public IActionResult Post([FromBody]DumpBundle bundle) {
			// TODO: create id for dump
			if (ModelState.IsValid) {
				// init again, in case request set these properties accidentally
				bundle.IsAnalysisComplete = false;
				bundle.HasAnalysisFailed = false;

				System.Diagnostics.Debug.WriteLine(bundle.Path);

				string filename = bundle.UrlFilename;
				//validate URL
				if (Utility.ValidateUrl(bundle.Url, ref filename)) {
					if (bundle.Id == null) {
						bundle.Id = Dumps.CreateUniqueBundleId();
					}
					bundle.UrlFilename = filename;
					Utility.ScheduleBundleAnalysis(PathHelper.GetWorkingDir(), bundle);

					// set response header for report entry
					//string link = Url.Action("BundleCreated", "Home", new { bundleId = bundle.Id });
					//Response.Headers.Add("Report", Request.Host + link);

					//bundle.Report = Request.Host + link;
					return CreatedAtAction("BundleCreated", "Home", new { bundleId = bundle.Id }, bundle);
				} else {
					return BadRequest("Invalid request, resource identifier is not valid or cannot be reached.");
				}
			} else {
				return BadRequest("Invalid request, check if value was set.");
			}
		}
	}
}
