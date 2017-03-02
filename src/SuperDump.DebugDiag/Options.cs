using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperDump.DebugDiag {
	public class Options {
		[Option("dump", HelpText = "Dump file for analysis", Required = true)]
		public string DumpFile { get; set; }

		[Option("out", HelpText = "Path for report file", Required = true)]
		public string ReportFile { get; set; }

		[Option("overwrite", HelpText = "Overwrite existing output files")]
		public bool Overwrite { get; set; }

		[Option("symbolPath", HelpText = "Symbol search path, e.g. srv*[local cache]*[private symbol server]*https://msdl.microsoft.com/download/symbols. If not specified _NT_SYMBOL_PATH is used")]
		public string SymbolPath { get; set; }

		[Option("imagePath", HelpText = "Image search path")]
		public string ImagePath { get; set; }

		[Option("analysis", HelpText = "List of DebugDiag analysis rules", Separator = ',')]
		public IEnumerable<string> AnalysisRules { get; set; }
	}
}
