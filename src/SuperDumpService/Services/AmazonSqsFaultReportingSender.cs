using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Hangfire;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SuperDumpService.Controllers;
using SuperDumpService.Helpers;
using SuperDumpService.Models;

namespace SuperDumpService.Services {
	public class AmazonSqsFaultReportingSender : IFaultReportSender {
		private readonly AmazonSqsSettings amazonSqsSettings;
		private readonly AmazonSqsClientService amazonSqsClientService;
		private readonly ILogger<AmazonSqsFaultReportingSender> logger;

		public AmazonSqsFaultReportingSender(
				IOptions<SuperDumpSettings> settings,
				AmazonSqsClientService amazonSqsClientService,
				ILoggerFactory loggerFactory
			) {
			this.amazonSqsSettings = settings.Value.AmazonSqsSettings;
			this.amazonSqsClientService = amazonSqsClientService;
			this.logger = loggerFactory.CreateLogger<AmazonSqsFaultReportingSender>();
		}

		public async Task SendFaultReport(DumpMetainfo dumpInfo, FaultReport faultReport) {
			var faultReportJson = JsonConvert.SerializeObject(faultReport);
			logger.LogInformation($"Sending to {amazonSqsSettings.FaultReportQueueUrl}: \n{faultReportJson}");
			var messageRequest = new SendMessageRequest(amazonSqsSettings.FaultReportQueueUrl, faultReportJson);
			await amazonSqsClientService.SqsClient.SendMessageAsync(messageRequest);
		}
	}
}
