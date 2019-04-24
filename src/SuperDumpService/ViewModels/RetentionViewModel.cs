using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SuperDumpService.Models;

namespace SuperDumpService.ViewModels {
	public class RetentionViewModel {
		public TimeSpan? RemainingRetentionTime { get; set; }
		public string RetentionTimeExtensionReason { get; set; }
		public bool IsDumpAvailable { get; set; }
		public bool IsNearDeletionDate { get; set; }

		public RetentionViewModel(DumpMetainfo dumpMetainfo, bool isDumpAvailable, TimeSpan warnBeforeDeletion) {
			RemainingRetentionTime = dumpMetainfo.PlannedDeletionDate - DateTime.Now;
			if (RemainingRetentionTime < TimeSpan.Zero) {
				RemainingRetentionTime = TimeSpan.Zero;
			}
			RetentionTimeExtensionReason = dumpMetainfo.RetentionTimeExtensionReason;
			IsDumpAvailable = isDumpAvailable;
			IsNearDeletionDate = RemainingRetentionTime < warnBeforeDeletion;
		}
	}
}
