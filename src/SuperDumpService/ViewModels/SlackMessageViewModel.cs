using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.ViewModels {

	public class SlackMessageViewModel {
		public string DumpFilename { get; set; }
		public List<string> TopProperties { get; set; } = new List<string>();
		public List<string> AgentModules { get; set; } = new List<string>();
		public string TopException { get; set; }
		public string Stacktrace { get; set; }
		public int NumManagedExceptions { get; internal set; }
		public int NumNativeExceptions { get; internal set; }
		public int NumAssertErrors { get; internal set; }
		public string Url { get; internal set; }
		public string LastEvent { get; internal set; }
	}
}
