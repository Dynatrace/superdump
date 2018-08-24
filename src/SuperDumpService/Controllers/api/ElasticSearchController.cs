using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SuperDumpService.Helpers;
using SuperDumpService.Services;
using System.Threading.Tasks;

namespace SuperDumpService.Controllers.Api {
	[Route("api/[controller]/{clean}")]
	public class ElasticSearchController : Controller {

		private readonly ElasticSearchService elasticService;
		private readonly ILogger<ElasticSearchController> logger;

		public ElasticSearchController(ElasticSearchService elasticService, ILoggerFactory loggerFactory) {
			this.elasticService = elasticService;
			logger = loggerFactory.CreateLogger<ElasticSearchController>();
		}

		[Authorize(Policy = LdapCookieAuthenticationExtension.AdminPolicy)]
		[HttpPost]
		[ProducesResponseType(typeof(void), 200)]
		public IActionResult PushElastic(bool clean) {
			logger.LogElasticClean("Elastic Search Clean", HttpContext, clean);
			Hangfire.BackgroundJob.Enqueue(() => elasticService.PushAllResultsAsync(clean));
			return Ok();
		}
	}
}
