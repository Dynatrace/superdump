﻿using SuperDump.Analyzer.Linux.Boundary;
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

		private readonly IFilesystem filesystem;
		private readonly SDCDSystemContext context;

		public ExecutablePathAnalyzer(IFilesystem filesystem, SDCDSystemContext context) {
			this.filesystem = filesystem ?? throw new ArgumentNullException("Filesystem must not be null!");
			this.context = context ?? throw new ArgumentNullException("System Context must not be null!");
		}

		public void Analyze() {
			context.FileName = ExecIfValid(PrependOrNull(".", GetExecutableFromSummary()))
				?? ExecIfValid(PrependOrNull(".", context.FileName))
				?? ExecIfValid(context.FileName)
				?? ExecIfValid(PrependOrNull(".", ExecFromArgs()))
				?? ExecIfValid(ExecFromArgs());
			Console.WriteLine("Executable File: " + context.FileName);
		}

		private string PrependOrNull(string prepend, string s) {
			if(s == null || prepend == null) {
				return null;
			}
			return prepend + s;
		}

		private string CharArrayToNullableString(char[] chars) {
			if(chars == null || chars.Length == 0) {
				return null;
			}
			return new string(chars);
		}

		private string ExecIfValid(string exec) {
			if (exec != null && filesystem.GetFile(exec).Exists) {
				return exec;
			}
			return null;
		}

		private string GetExecutableFromSummary() {
			IFileInfo summaryTxt = filesystem.GetFile(Constants.SUMMARY_TXT);
			if (!summaryTxt.Exists) {
				return null;
			}

			return filesystem.ReadLines(summaryTxt).Select(line => {
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
