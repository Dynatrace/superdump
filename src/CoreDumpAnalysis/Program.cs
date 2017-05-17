using System;
using System.IO;

using SuperDump.Models;
using SuperDump.Analyzers;

namespace CoreDumpAnalysis {
	public class Program {
		static void Main(string[] args) {
			Console.WriteLine("SuperDump - Dump analysis tool");
			Console.WriteLine("--------------------------");
			if (args.Length == 2) {
				Console.WriteLine("Input File: " + args[0]);
				Console.WriteLine("Output File: " + args[1]);
				new Program().AnalyzeDirectory(args[0], args[1]);
			} else {
				Console.WriteLine("Invalid argument count! CoreDumpAnalysis <coredump> <working-dir>");
			}
		}

		private void AnalyzeDirectory(string inputFile, string outputFile) {
			string coredump = GetCoreDumpFilePath(inputFile);
			if (coredump == null) {
				Console.WriteLine("No core dump found.");
				// TODO write empty json?
				return;
			}
			Console.WriteLine("Processing core dump file: " + coredump);

			SDResult analysisResult = new SDResult();
			new UnwindAnalysis(coredump, analysisResult).DebugAndSetResultFields();
			Console.WriteLine("Resolving debug symbols ...");
			new DebugSymbolAnalysis(coredump, analysisResult).DebugAndSetResultFields();
			Console.WriteLine("Setting tags ...");
			new TagAnalyzer(analysisResult).Analyze();
			Console.WriteLine("Setting default fields ...");
			new DefaultFieldsSetter(coredump, analysisResult).DebugAndSetResultFields();
			File.WriteAllText(outputFile, analysisResult.SerializeToJSON());
			Console.WriteLine("Finished coredump analysis.");
		}

		private String GetCoreDumpFilePath(string inputFile) {
			string directory = FilesystemHelper.GetParentDirectory(inputFile);
			if (!File.Exists(inputFile)) {
				Console.WriteLine("Input file " + inputFile + " does not exist on the filesystem. Searching for a coredump in the directory...");
				return FindCoredumpOrNull(directory);
			} else if (inputFile.EndsWith(".tar") || inputFile.EndsWith(".gz") || inputFile.EndsWith(".tgz") || inputFile.EndsWith(".tar") || inputFile.EndsWith(".zip")) {
				Console.WriteLine("Extracting archives in directory " + directory);
				ExtractArchivesInDir(directory);
				return FindCoredumpOrNull(directory);
			} else if (inputFile.EndsWith(".core")) {
				return inputFile;
			} else {
				Console.WriteLine("Failed to interpret input file. Assuming it is a core dump.");
				return inputFile;
			}
		}

		private void ExtractArchivesInDir(String directory) {
			bool workDone = true;
			while (workDone) {
				workDone = false;
				foreach (String file in FilesystemHelper.FilesInDirectory(directory)) {
					workDone |= ArchiveHelper.TryExtract(file);
				}
			}
		}

		private String FindCoredumpOrNull(String directory) {
			foreach (String file in FilesystemHelper.FilesInDirectory(directory)) {
				if (file.EndsWith(".core")) {
					return file;
				}
			}
			return null;
		}
	}
}
 