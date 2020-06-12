using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SuperDump.Models;
using SuperDumpService.Models;

namespace SuperDumpService.Services {
	/// <summary>
	/// This service creates a report from a crash dump, that is supposed to identify it well enough to be able to detect identical crashes (deduplication).
	/// The report is supposed to be consumed by an expternal service, not by SuperDump itself.
	/// </summary>
	public class FaultReportingService {
		private readonly IFaultReportSender faultReportSender;
		private readonly DumpRepository dumpRepository;
		private readonly BundleRepository bundleRepository;

		public FaultReportingService(IFaultReportSender faultReportSender, DumpRepository dumpRepository, BundleRepository bundleRepository) {
			this.faultReportSender = faultReportSender;
			this.dumpRepository = dumpRepository;
			this.bundleRepository = bundleRepository;
		}

		public async Task PublishFaultReport(DumpMetainfo dumpInfo) {
			var result = await dumpRepository.GetResult(dumpInfo.Id);
			var faultReport = FaultReportCreator.CreateFaultReport(result);
			var bundleInfo = bundleRepository.Get(dumpInfo.BundleId);
			if (bundleInfo.CustomProperties.ContainsKey("sourceId")) faultReport.SourceId = bundleRepository.Get(dumpInfo.BundleId).CustomProperties["sourceId"];
			await faultReportSender.SendFaultReport(dumpInfo, faultReport);
		}
	}

	public static class FaultReportCreator {

		public static FaultReport CreateFaultReport(SDResult result, int maxFrames = 80) {
			var faultReport = new FaultReport();

			// modules & stackframes
			var faultingThread = result.GetErrorOrLastExecutingThread();
			if (faultingThread != null) {
				faultReport.FaultingFrames = new List<string>();

				if (faultingThread.StackTrace.Count <= maxFrames) {
					foreach (var frame in faultingThread.StackTrace) {
						faultReport.FaultingFrames.Add(frame.ToString());
					}
				} else {
					// makes ure extra long stacktraces (stack overflows) are serialized in a human readable way
					// add only the first X, then "...", then the last X
					foreach (var frame in faultingThread.StackTrace.Take(maxFrames/2)) {
						faultReport.FaultingFrames.Add(frame.ToString());
					}
					faultReport.FaultingFrames.Add("...");
					foreach (var frame in faultingThread.StackTrace.TakeLast(maxFrames/2)) {
						faultReport.FaultingFrames.Add(frame.ToString());
					}
				}
			}

			var faultReasonSb = new StringBuilder();
			// lastevent
			if (result.LastEvent != null) {
				if (result.LastEvent.Description.StartsWith("Break instruction exception")) {
					// "break instruction" as a lastevent is so generic, it's practically useless. treat it as if there was no information at all.
				} else {
					if (!string.IsNullOrEmpty(result.LastEvent.Type)) {
						faultReasonSb.Append(result.LastEvent.Type);
					}
					if (!string.IsNullOrEmpty(result.LastEvent.Description)) {
						if (faultReasonSb.Length > 0) faultReasonSb.Append(", ");
						faultReasonSb.Append(result.LastEvent.Description);
					}
				}
			}

			// exception
			var exception = result.GetErrorOrLastExecutingThread()?.LastException;
			if (exception != null) {
				if (!string.IsNullOrEmpty(exception.Type)) {
					if (faultReasonSb.Length > 0) faultReasonSb.Append(", ");
					faultReasonSb.Append(exception.Type);
				}
				if (!string.IsNullOrEmpty(exception.Message)) {
					if (faultReasonSb.Length > 0) faultReasonSb.Append(", ");
					faultReasonSb.Append(exception.Message);
				}
			}
			faultReport.FaultReason = faultReasonSb.ToString();

			return faultReport;
		}
	}

	/// <summary>
	/// A FaultReport is supposed to be a concise summary of the crash reason. Human readable.
	/// </summary>
	public class FaultReport {
		public string SourceId { get; set; }
		public string FaultReason { get; set; }
		public string FaultLocation { get; set; }

		public List<string> FaultingFrames { get; set; }
		
		public override string ToString() {
			return $"{FaultReason}\n{FaultLocation}\n\n{string.Join('\n', FaultingFrames)}";
		}
	}

	/// <summary>
	/// A dummy FaultReport sender, in case no other sender is registered.
	/// </summary>
	public class ConsoleFaultReportSender : IFaultReportSender {
		public Task SendFaultReport(DumpMetainfo dumpInfo, FaultReport faultReport) {
			Console.WriteLine("FaultReport: " + dumpInfo.ToString() + " " + faultReport.ToString());
			return Task.CompletedTask;
		}
	}
}
