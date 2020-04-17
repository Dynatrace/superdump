using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SuperDumpService.Models;

namespace SuperDumpService.Services.Analyzers {
	public class FaultReportJob : PostAnalysisJob {
		private readonly FaultReportingService faultReportingService;
		private readonly IOptions<SuperDumpSettings> settings;

		public FaultReportJob(FaultReportingService faultReportingService, IOptions<SuperDumpSettings> settings) {
			this.faultReportingService = faultReportingService;
			this.settings = settings;
		}

		public override async Task AnalyzeDump(DumpMetainfo dumpInfo) {
			await faultReportingService.PublishFaultReport(dumpInfo);
		}
	}
}
