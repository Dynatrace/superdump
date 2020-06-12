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
	public class AmazonSqsPollingService {
		private readonly AmazonSqsSettings amazonSqsSettings;
		private readonly SuperDumpRepository superDumpRepo;
		private readonly AmazonSqsClientService amazonSqsClientService;
		private readonly LinkGenerator linkGenerator;
		private readonly ILogger<AmazonSqsPollingService> logger;
		private readonly Uri baseUri;

		public AmazonSqsPollingService(
				IOptions<SuperDumpSettings> settings,
				SuperDumpRepository superDumpRepo,
				AmazonSqsClientService amazonSqsClientService,
				LinkGenerator linkGenerator, 
				ILoggerFactory loggerFactory
			) {
			this.amazonSqsSettings = settings.Value.AmazonSqsSettings;
			this.superDumpRepo = superDumpRepo;
			this.amazonSqsClientService = amazonSqsClientService;
			this.linkGenerator = linkGenerator;
			this.logger = loggerFactory.CreateLogger<AmazonSqsPollingService>();
			this.baseUri = new Uri(amazonSqsSettings.SuperDumpBaseUrl);
		}


		public void StartHangfireJob() {
			RecurringJob.AddOrUpdate(() => PollQueue(), amazonSqsSettings.PollCron);
		}

		[Queue("amazon-sqs-poll")]
		public void PollQueue() {
			AsyncHelper.RunSync(PollQueueAsync);
		}

		public async Task PollQueueAsync() {
			var request = new ReceiveMessageRequest(amazonSqsSettings.InputQueueUrl) {
				MaxNumberOfMessages = amazonSqsSettings.MessagesPerReceive,
				VisibilityTimeout = (int)amazonSqsSettings.MessageVisibilityTimeout.TotalSeconds
			};

			int received;

			do {
				ReceiveMessageResponse response = await amazonSqsClientService.SqsClient.ReceiveMessageAsync(request);

				received = response.Messages.Count;

				if (received > 0) {
					await ProcessMessageBatch(response.Messages);
				}
			} while (received >= amazonSqsSettings.MessagesPerReceive);
		}

		/// <summary>
		/// Processes a message batch, sends response messages and deletes all handled messages.
		/// 
		/// Received messages are deleted from the queue even if an error occurs while handling them.
		/// This is to avoid filling the queue with invalid messages.
		/// </summary>
		/// <param name="messages"></param>
		/// <returns></returns>
		private async Task ProcessMessageBatch(List<Message> messages) {
			var deleteEntries = new List<DeleteMessageBatchRequestEntry>(messages.Count);
			var responseEntries = new List<SendMessageBatchRequestEntry>(messages.Count);
			int i = 0;

			foreach (Message message in messages) {
				DumpAnalysisResponse result = null;

				try {
					result = ProcessMessage(message);
				} catch (Exception e) {
					Console.WriteLine(e);
					logger.LogSqsException(message.Body, e);
				}

				if (result != null) {
					responseEntries.Add(new SendMessageBatchRequestEntry(i.ToString(), JsonConvert.SerializeObject(result)));
				}

				deleteEntries.Add(new DeleteMessageBatchRequestEntry(i.ToString(), message.ReceiptHandle));
				i++;
			}

			if (responseEntries.Count > 0) {
				await amazonSqsClientService.SqsClient.SendMessageBatchAsync(new SendMessageBatchRequest(amazonSqsSettings.OutputQueueUrl, responseEntries));
			}
			if (deleteEntries.Count > 0) {
				await amazonSqsClientService.SqsClient.DeleteMessageBatchAsync(new DeleteMessageBatchRequest(amazonSqsSettings.InputQueueUrl, deleteEntries));
			}
		}

		/// <summary>
		/// Processes a single message.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		private DumpAnalysisResponse ProcessMessage(Message message) {
			AwsDumpAnalysisInput dumpInput = JsonConvert.DeserializeObject<AwsDumpAnalysisInput>(message.Body);
			string bundleId = superDumpRepo.ProcessWebInputfile(dumpInput);

			if (!string.IsNullOrEmpty(bundleId)) {
				logger.LogSqsFileUpload(bundleId, dumpInput);
				string url = new Uri(baseUri, linkGenerator.GetPathByAction(nameof(HomeController.BundleCreated), "Home", new { bundleId })).ToString();

				return new DumpAnalysisResponse(dumpInput.SourceId, url);
			} else {
				logger.LogSqsInvalidUrl(dumpInput);
				return null;
			}
		}
	}
}
