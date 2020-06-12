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
using Newtonsoft.Json.Serialization;
using SuperDumpService.Controllers;
using SuperDumpService.Helpers;
using SuperDumpService.Models;

namespace SuperDumpService.Services {
	public class AmazonSqsFaultReportingSender : IFaultReportSender {
		private readonly string queueUrl;
		private readonly AmazonSqsClientService amazonSqsClientService;
		private readonly ILogger<AmazonSqsFaultReportingSender> logger;

		public AmazonSqsFaultReportingSender(
				IOptions<SuperDumpSettings> settings,
				AmazonSqsClientService amazonSqsClientService,
				ILoggerFactory loggerFactory
			) {
			this.queueUrl = settings.Value.AmazonSqsSettings.FaultReportQueueUrl;
			this.amazonSqsClientService = amazonSqsClientService;
			this.logger = loggerFactory.CreateLogger<AmazonSqsFaultReportingSender>();
		}

		public async Task SendFaultReport(DumpMetainfo dumpInfo, FaultReport faultReport) {
			var faultReportJson = JsonConvert.SerializeObject(faultReport, new JsonSerializerSettings {
				ContractResolver = new CamelCasePropertyNamesContractResolver(),
			});
			logger.LogInformation($"Sending to {queueUrl}: \n{faultReportJson}");
			var messageRequest = new SendMessageRequest(queueUrl, faultReportJson);
			try {
				await amazonSqsClientService.SqsClient.SendMessageAsync(messageRequest);
			} catch (Exception e) {
				logger.LogWarning($"Could not send message to queue {queueUrl}. Error: {e}");
			}
		}
	}
}
