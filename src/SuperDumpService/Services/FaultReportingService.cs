using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SuperDumpService.Models;

namespace SuperDumpService.Services {
	/// <summary>
	/// This service creates a report from a crash dump, that is supposed to identify it well enough to be able to detect identical crashes (deduplication).
	/// The report is supposed to be consumed by an expternal service, not by SuperDump itself.
	/// </summary>
	public class FaultReportingService {
		private readonly IFaultReportSender faultReportSender;

		public FaultReportingService(IFaultReportSender faultReportSender) {
			this.faultReportSender = faultReportSender;
		}

		public async Task PublishFaultReport(DumpMetainfo dumpInfo) {
			var faultReport = CreateFaultReport(dumpInfo);
			await faultReportSender.SendFaultReport(faultReport);
		}

		private FaultReport CreateFaultReport(DumpMetainfo dumpInfo) {
			return new FaultReport(dumpInfo);
		}
	}

	public class FaultReport {
		private DumpMetainfo dumpInfo;

		public FaultReport(DumpMetainfo dumpInfo) {
			this.dumpInfo = dumpInfo;
		}

		public override string ToString() {
			return dumpInfo.ToString();
		}
	}

	/// <summary>
	/// A dummy FaultReport sender, in case no other sender is registered.
	/// </summary>
	public class ConsoleFaultReportingSender : IFaultReportSender {
		public async Task SendFaultReport(FaultReport faultReport) {
			Console.WriteLine(faultReport.ToString());
		}
	}
}
