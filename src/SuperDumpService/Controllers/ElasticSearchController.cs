using Microsoft.AspNetCore.Mvc;
using SuperDumpService.Services;
using System.Threading.Tasks;

namespace SuperDumpService.Controllers.Api {
	[Route("api/[controller]")]
	public class ElasticSearchController : Controller {

		private readonly ElasticSearchService elasticService;

		public ElasticSearchController(ElasticSearchService elasticService) {
			this.elasticService = elasticService;
		}

		[HttpPost]
		[ProducesResponseType(typeof(void), 200)]
		public IActionResult PushElastic() {
			Hangfire.BackgroundJob.Enqueue(() => elasticService.PushAllResultsAsync());
			return Ok();
		}
	}
}
