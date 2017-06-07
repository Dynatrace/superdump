using SuperDump.Analyzer.Linux.Boundary;
using SuperDump.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SuperDump.Analyzer.Linux.Analysis {
	public class CoreLogAnalyzer {
		private static Regex VERSION_REGEX = new Regex("\\(([\\w-\\.]+)\\)$", RegexOptions.Compiled);

		private readonly SDResult analysisResult;
		private readonly IFilesystem filesystem;
		private readonly string coredump;

		public CoreLogAnalyzer(IFilesystem filesystem, string coredump, SDResult analysisResult) {
			this.analysisResult = analysisResult ?? throw new ArgumentNullException("Analysis Result must not be null!");
			this.filesystem = filesystem ?? throw new ArgumentNullException("Filesystem must not be null!");
			this.coredump = coredump ?? throw new ArgumentNullException("Coredump path must not be null!");
		}

		public void Analyze() {
			string logPath = $"{Path.Combine(Path.GetDirectoryName(coredump), Path.GetFileNameWithoutExtension(coredump))}.log";
			if (!filesystem.FileExists(logPath)) {
				Console.WriteLine($"No coredump log available ({logPath}). Skipping.");
				return;
			}
			IEnumerable<string> lines = filesystem.ReadLines(logPath);
			foreach (SDModule module in analysisResult.SystemContext.Modules) {
				SetVersionIfAvailable(module, lines);
			}
		}

		private void SetVersionIfAvailable(SDModule module, IEnumerable<string> lines) {
			foreach (string line in lines) {
				if (line.Contains(module.FileName)) {
					Match match = VERSION_REGEX.Match(line);
					if (match.Success) {
						module.Version = match.Groups[1].Value;
						return;
					}
				}
			}
		}
	}
}
