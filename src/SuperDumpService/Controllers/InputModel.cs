using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.Controllers {
	public class DumpAnalysisInput {

		public DumpAnalysisInput() {
			// keep default ctor for deserializer
		}

		// just a convenience ctor
		public DumpAnalysisInput(string url, params Tuple<string, string>[] customProperties) {
			this.Url = url;
			if (customProperties != null) {
				foreach (var prop in customProperties) {
					CustomProperties[prop.Item1] = prop.Item2;
				}
			}
		}

		/// <summary>
		/// Path to a local file, or a http url.
		/// Accepted filetypes: .dmp, .zip, .dll, .pdb
		/// </summary>
		public string Url { get; set; }

		/// <summary>
		/// Optional. If provided, it's used as filename to store the dump.
		/// </summary>
		public string UrlFilename { get; set; }

		// optional key/value list of additional info
		public Dictionary<string, string> CustomProperties { get; set; } = new Dictionary<string, string>();

		// for compat. use CustomProperties instead!
		public string JiraIssue { get; set; }

		// for compat. use CustomProperties instead!
		public string FriendlyName { get; set; }
	}
}
