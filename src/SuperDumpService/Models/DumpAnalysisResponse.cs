using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.Models {
	public class DumpAnalysisResponse {
		public string SourceId { get; }
		public string SuperdumpUrl { get; }

		public DumpAnalysisResponse(string sourceId, string superDumpUrl) {
			SourceId = sourceId;
			SuperdumpUrl = superDumpUrl;
		}
	}
}
