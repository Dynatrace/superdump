using SuperDumpService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.Services {
	public class NotificationService {
		private SlackNotificationService slackNotificationService;

		public NotificationService(SlackNotificationService slackNotificationService) {
			this.slackNotificationService = slackNotificationService;
		}

		public async Task NotifyDumpAnalysisFinished(DumpMetainfo dumpInfo) {
			await slackNotificationService.NotifyDumpAnalysisFinished(dumpInfo);
		}
	}
}
