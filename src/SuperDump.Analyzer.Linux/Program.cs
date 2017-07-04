using System;

using SuperDump.Analyzer.Linux.Boundary;
using SuperDump.Analyzer.Linux.Analysis;
using System.Collections.Generic;
using SuperDump.Common;

namespace SuperDump.Analyzer.Linux {
	public class Program {
		private const string EXPECTED_COMMAND = "CoreDumpAnalysis [-prepare] coredump-file|coredump-dir [output-file]";

		private static readonly IFilesystem filesystem = new Filesystem();
		private static readonly IArchiveHandler archiveHandler = new ArchiveHandler(filesystem);
		private static readonly IProcessHandler processHandler = new ProcessHandler();
		private static readonly IHttpRequestHandler requestHandler = new HttpRequestHandler(filesystem);

		public static int Main(string[] args) {
			Console.WriteLine("SuperDump - Dump analysis tool");
			Console.WriteLine("--------------------------");
			var (arguments,options) = GetCommands(args);
			if(options.Contains("-prepare")) {
				if (arguments.Count < 1 || arguments.Count > 2) {
					Console.WriteLine($"Invalid argument count! {EXPECTED_COMMAND}");
					return LinuxAnalyzerExitCode.InvalidArguments.Code;
				}
				Prepare(arguments[0]);
			} else {
				if(arguments.Count != 2) {
					Console.WriteLine($"Invalid argument count! {EXPECTED_COMMAND}");
					return (int) LinuxAnalyzerExitCode.InvalidArguments.Code;
				}
				return RunAnalysis(arguments[0], arguments[1]).Code;
			}
			return 1;
		}

		public static (IList<string>,IList<string>) GetCommands(string[] args) {
			var commands = new List<string>();
			var options = new List<string>();
			for (int i = 0; i < args.Length; i++) {
				if (args[i].StartsWith("-")) {
					options.Add(args[i]);
				} else {
					commands.Add(args[i]);
				}
			}
			return (commands, options);
		}

		private static LinuxAnalyzerExitCode RunAnalysis(string input, string output) {
			var analysis = new CoreDumpAnalyzer(archiveHandler, filesystem, processHandler, requestHandler).AnalyzeAsync(input, output);
			analysis.Wait();
			return analysis.Result;
		}

		private static void Prepare(string input) {
			new CoreDumpAnalyzer(archiveHandler, filesystem, processHandler, requestHandler).Prepare(input);
		}
	}
}
