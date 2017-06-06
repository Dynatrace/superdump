using SuperDump.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace CoreDumpAnalysis.analysis {
	public class ExecutablePathAnalyzer {

		private static Regex EXECUTABLE_REGEX = new Regex("executablePath: ([^\\s]+)");

		private readonly SDCDSystemContext context;
		private readonly IFilesystem filesystem;

		public ExecutablePathAnalyzer(IFilesystem filesystem, SDResult analysisResult) {
			this.filesystem = filesystem ?? throw new ArgumentNullException("Filesystem must not be null!");
			this.context = analysisResult?.SystemContext as SDCDSystemContext ?? throw new ArgumentNullException("Analysis Result must not be null!");
		}

		public void Analyze() {
			context.FileName = ExecIfValid("." + GetExecutableFromSummary())
				?? ExecIfValid("." + context.FileName)
				?? ExecIfValid(context.FileName)
				?? ExecIfValid("." + ExecFromArgs())
				?? ExecIfValid(ExecFromArgs());
			Console.WriteLine("Executable File: " + context.FileName);
		}

		private string ExecIfValid(string exec) {
			Console.WriteLine("Checking " + exec);
			if (exec != null && filesystem.FileExists(exec)) {
				Console.WriteLine("Valid!");
				return exec;
			}
			return null;
		}

		private string GetExecutableFromSummary() {
			IEnumerable<string> lines = filesystem.ReadLines(Constants.SUMMARY_TXT);
			foreach (string line in lines) {
				Match match = EXECUTABLE_REGEX.Match(line);
				if (match.Success) {
					return match.Groups[1].Value;
				}
			}
			return null;
		}

		private string ExecFromArgs() {
			string execWithArgs = context.Args;
			int firstSpace = execWithArgs?.IndexOf(' ') ?? -1;
			if (firstSpace >= 0) {
				return execWithArgs.Substring(0, firstSpace);
			}
			return execWithArgs;
		}
	}
}
