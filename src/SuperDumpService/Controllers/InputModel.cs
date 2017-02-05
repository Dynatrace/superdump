using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.Controllers {
	public class DumpAnalysisInput {
		/// <summary>
		/// Path to a local file, or a http url.
		/// Accepted filetypes: .dmp, .zip, .dll, .pdb
		/// </summary>
		public string Url { get; set; }

		/// <summary>
		/// Optional. If provided, it's used as filename to store the dump.
		/// </summary>
		public string Filename { get; set; }

		// for compat
		public string JiraIssue { get; set; }

	}
}
