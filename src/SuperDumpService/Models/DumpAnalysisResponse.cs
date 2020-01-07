using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SuperDumpService.Models {
	public class DumpAnalysisResponse {
		[JsonProperty("sourceId")]
		public string SourceId { get; }
		[JsonProperty("superdumpUrl")]
		public string SuperdumpUrl { get; }

		public DumpAnalysisResponse(string sourceId, string superDumpUrl) {
			SourceId = sourceId;
			SuperdumpUrl = superDumpUrl;
		}
	}
}
