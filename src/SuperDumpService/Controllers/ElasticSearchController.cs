using Microsoft.AspNetCore.Mvc;
using SuperDumpService.Services;
using System.Threading.Tasks;

namespace SuperDumpService.Controllers.Api {
	[Route("api/[controller]/{clean}")]
	public class ElasticSearchController : Controller {

		private readonly ElasticSearchService elasticService;

		public ElasticSearchController(ElasticSearchService elasticService) {
			this.elasticService = elasticService;
		}

		[HttpPost]
		[ProducesResponseType(typeof(void), 200)]
		public IActionResult PushElastic(bool clean) {
			Hangfire.BackgroundJob.Enqueue(() => elasticService.PushAllResultsAsync(clean));
			return Ok();
		}
	}
}
