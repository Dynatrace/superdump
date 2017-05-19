﻿using System;
using System.IO;

using SuperDump.Models;
using SuperDump.Analyzers;
using CoreDumpAnalysis.boundary;

namespace CoreDumpAnalysis {
	public class Program {
		public static void Main(string[] args) {
			Console.WriteLine("SuperDump - Dump analysis tool");
			Console.WriteLine("--------------------------");
			if (args.Length == 2) {
				Console.WriteLine("Input File: " + args[0]);
				Console.WriteLine("Output File: " + args[1]);
				RunAnalysis(args[0], args[1]);
			} else {
				Console.WriteLine("Invalid argument count! CoreDumpAnalysis <coredump> <working-dir>");
			}
		}

		private static void RunAnalysis(string input, string output) {
			IFilesystem filesystem = new Filesystem();
			IArchiveHandler archiveHandler = new ArchiveHandler(filesystem);
			IProcessHandler processHandler = new ProcessHandler();
			IHttpRequestHandler requestHandler = new HttpRequestHandler(filesystem);
			new CoreDumpAnalysis(archiveHandler, filesystem, processHandler, requestHandler).AnalyzeDirectory(input, output);
		}
	}
}
 