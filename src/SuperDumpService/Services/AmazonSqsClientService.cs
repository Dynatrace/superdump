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
	public class AmazonSqsClientService {
		private readonly AmazonSqsSettings amazonSqsSettings;
		public IAmazonSQS SqsClient { get; }

		public AmazonSqsClientService(IOptions<SuperDumpSettings> settings) {
			this.amazonSqsSettings = settings.Value.AmazonSqsSettings;
			var credentials = new BasicAWSCredentials(amazonSqsSettings.AccessKey, amazonSqsSettings.SecretKey);
			var config = new AmazonSQSConfig {
				RegionEndpoint = RegionEndpoint.GetBySystemName(amazonSqsSettings.Region)
			};
			this.SqsClient = new AmazonSQSClient(credentials, config);
		}
	}
}
