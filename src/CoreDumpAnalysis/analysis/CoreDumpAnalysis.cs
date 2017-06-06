using CoreDumpAnalysis.analysis;
using CoreDumpAnalysis.boundary;
using SuperDump.Analyzers;
using SuperDump.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CoreDumpAnalysis {
	public class CoreDumpAnalysis {
		private readonly IArchiveHandler archiveHandler;
		private readonly IFilesystem filesystem;
		private readonly IProcessHandler processHandler;
		private readonly IHttpRequestHandler requestHandler;

		public CoreDumpAnalysis(IArchiveHandler archiveHandler, IFilesystem filesystem, IProcessHandler processHandler, IHttpRequestHandler requestHandler) {
			this.archiveHandler = archiveHandler ?? throw new ArgumentNullException("ArchiveHandler must not be null!");
			this.filesystem = filesystem ?? throw new ArgumentNullException("Filesystem must not be null!");
			this.processHandler = processHandler ?? throw new ArgumentNullException("ProcessHandler must not be null!");
			this.requestHandler = requestHandler ?? throw new ArgumentNullException("RequestHandler must not be null!");
		}

		public void AnalyzeDirectory(string inputFile, string outputFile) {
			string coredump = GetCoreDumpFilePath(inputFile);
			if (coredump == null) {
				Console.WriteLine("No core dump found.");
				// TODO write empty json?
				return;
			}
			Console.WriteLine("Processing core dump file: " + coredump);

			SDResult analysisResult = new SDResult();
			new UnwindAnalyzer(filesystem, coredump, analysisResult).DebugAndSetResultFields();
			Console.WriteLine("Finding executable file ...");
			new ExecutablePathAnalyzer(filesystem, analysisResult).Analyze();
			Console.WriteLine("Retrieving agent version if available ...");
			new CoreLogAnalyzer(filesystem, coredump, analysisResult).DebugAndSetResultFields();
			Console.WriteLine("Fetching debug symbols ...");
			new DebugSymbolResolver(filesystem, requestHandler).Resolve(analysisResult.SystemContext.Modules);
			Console.WriteLine("Resolving debug symbols ...");
			new DebugSymbolAnalyzer(filesystem, processHandler, coredump, analysisResult).DebugAndSetResultFields();
			Console.WriteLine("Setting tags ...");
			new TagAnalyzer(analysisResult).Analyze();
			Console.WriteLine("Reading stack information ...");
			new GdbAnalyzer(filesystem, processHandler, coredump, analysisResult).DebugAndSetResultFields();
			Console.WriteLine("Setting default fields ...");
			new DefaultFieldsSetter(analysisResult).SetResultFields();
			File.WriteAllText(outputFile, analysisResult.SerializeToJSON());
			Console.WriteLine("Finished coredump analysis.");
		}

		private String GetCoreDumpFilePath(string inputFile) {
			string directory = filesystem.GetParentDirectory(inputFile);
			if (!filesystem.FileExists(inputFile)) {
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
				foreach (String file in filesystem.FilesInDirectory(directory)) {
					workDone |= archiveHandler.TryExtract(file);
				}
			}
		}

		private String FindCoredumpOrNull(String directory) {
			foreach (String file in filesystem.FilesInDirectory(directory)) {
				if (file.EndsWith(".core")) {
					return file;
				}
			}
			return null;
		}
	}
}
