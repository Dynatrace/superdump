using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperDumpSelector {
	public class Options {
		[Option("dump", HelpText = "Path do dump file", Required = true)]
		public string DumpFile { get; set; }

		[Option("out", HelpText = "Output file (json)", Required = true)]
		public string OutputFile { get; set; }

		[Option("tracetag", HelpText = "dynatrace tracetag", Required = false)]
		public string TraceTag { get; set; }
	}
}
