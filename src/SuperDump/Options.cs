using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperDump {
	public class Options {
		[Option("dump", HelpText = "Path do dump file", Required = true)]
		public string DumpFile { get; set; }

		[Option("out", HelpText = "Output file (json)", Required = true)]
		public string OutputFile { get; set; }
	}
}
