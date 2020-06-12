using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService {
	public class AmazonSqsSettings {
		public string SuperDumpBaseUrl { get; set; }
		public string PollCron { get; set; }
		public int MessagesPerReceive { get; set; } = 10;
		public TimeSpan MessageVisibilityTimeout { get; set; }
		public string Region { get; set; }
		public string InputQueueUrl { get; set; }
		public string OutputQueueUrl { get; set; }
		public string FaultReportQueueUrl { get; set; }
		public string AccessKey { get; set; }
		public string SecretKey { get; set; }
	}
}
