using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SuperDump.Analyzer.Linux.Boundary;
using SuperDump.Common;
using Thinktecture.IO;

namespace SuperDump.Analyzer.Linux.Analysis {
	class ElfHeaderAnalyzer {
		private static readonly Regex ArchitectureRegex = new Regex("^\\s*Machine:\\s*(.+)$", RegexOptions.Multiline);

		private readonly IFileInfo coredump;
		private readonly Models.SDResult analysisResult;

		public ElfHeaderAnalyzer(IFileInfo coredump, Models.SDResult analysisResult) {
			this.coredump = coredump ?? throw new ArgumentNullException("Coredump must not be null!");
			this.analysisResult = analysisResult ?? throw new ArgumentNullException("Analysis result must not be null!");
		}

		/// <summary>
		/// Sets the system architecture to the "Machine" field of the ELF header, as returned by readelf -h.
		/// </summary>
		/// <returns></returns>
		public async Task Analyze() {
			using (ProcessRunner proc = await ProcessRunner.Run("readelf", new DirectoryInfo(Directory.GetCurrentDirectory()), "-h " + coredump.FullName)) {
				if (proc.ExitCode != 0) {
					Console.WriteLine($"readelf -h exited with error: {proc.StdErr}");
				}

				Match match = ArchitectureRegex.Match(proc.StdOut);
				if (match.Success) {
					analysisResult.SystemContext.SystemArchitecture = MapArchitectureDescription(match.Groups[1].Value);
				}
			}
		}

		private string MapArchitectureDescription(string architecture) {
			if (architecture == "Intel 80386") {
				return "x86";
			}

			if (architecture == "Advanced Micro Devices X86-64") {
				return "Amd64";
			}

			return architecture + " (Unsupported)";
		}
	}
}
