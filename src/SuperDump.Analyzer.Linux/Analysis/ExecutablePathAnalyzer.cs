using SuperDump.Analyzer.Linux.Boundary;
using SuperDump.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Thinktecture.IO;
using Thinktecture.IO.Adapters;
using System.Linq;

namespace SuperDump.Analyzer.Linux.Analysis {
	public class ExecutablePathAnalyzer {

		private static Regex EXECUTABLE_REGEX = new Regex("executablePath: ([^\\s]+)", RegexOptions.Compiled);

		private readonly SDCDSystemContext context;

		public ExecutablePathAnalyzer(SDResult analysisResult) {
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
			if (exec != null && new FileInfoAdapter(exec).Exists) {
				return exec;
			}
			return null;
		}

		private string GetExecutableFromSummary() {
			IFileInfo summaryTxt = new FileInfoAdapter(Constants.SUMMARY_TXT);
			if (!summaryTxt.Exists) {
				return null;
			}

			return new FileAdapter().ReadLines(Constants.SUMMARY_TXT).Select(line => {
				Match match = EXECUTABLE_REGEX.Match(line);
				if (match.Success) {
					return match.Groups[1].Value;
				}
				return "";
			}).FirstOrDefault(match => match != "");
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
