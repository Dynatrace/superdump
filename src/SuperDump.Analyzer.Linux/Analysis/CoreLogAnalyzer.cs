using SuperDump.Analyzer.Linux.Boundary;
using SuperDump.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Thinktecture.IO;

namespace SuperDump.Analyzer.Linux.Analysis {
	public class CoreLogAnalyzer {
		private static Regex VERSION_REGEX = new Regex("\\(([\\w-\\.]+)\\)$", RegexOptions.Compiled);

		private readonly IList<SDModule> modules;
		private readonly IFilesystem filesystem;
		private readonly IFileInfo coredump;

		public CoreLogAnalyzer(IFilesystem filesystem, IFileInfo coredump, IList<SDModule> modules) {
			this.modules = modules ?? throw new ArgumentNullException("Modules must not be null!");
			this.filesystem = filesystem ?? throw new ArgumentNullException("Filesystem must not be null!");
			this.coredump = coredump ?? throw new ArgumentNullException("Coredump path must not be null!");
		}

		public void Analyze() {
			string logPath = $"{Path.Combine(coredump.Directory.FullName, Path.GetFileNameWithoutExtension(coredump.FullName))}.log";
			var logFile = filesystem.GetFile(logPath);
			if (!logFile.Exists) {
				Console.WriteLine($"No coredump log available ({logPath}). Skipping.");
				return;
			}
			SetVersionsIfAvailable(logFile);
		}

		private void SetVersionsIfAvailable(IFileInfo logPath) {
			foreach (string line in filesystem.ReadLines(logPath)) {
				Match match = VERSION_REGEX.Match(line);
				if (match.Success) {
					foreach (SDModule module in modules) {
						if (line.Contains(module.FileName)) {
							module.Version = match.Groups[1].Value;
							break;
						}
					}
				}
			}
		}
	}
}
