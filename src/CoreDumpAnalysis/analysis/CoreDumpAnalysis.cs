using SuperDump.Analyzers;
using SuperDump.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CoreDumpAnalysis {
	public class CoreDumpAnalysis {
		private readonly IArchiveHelper archiveHelper;
		private readonly IFilesystemHelper filesystemHelper;
		private readonly IProcessHelper processHelper;

		public CoreDumpAnalysis(IArchiveHelper archiveHelper, IFilesystemHelper filesystemHelper, IProcessHelper processHelper) {
			this.archiveHelper = archiveHelper ?? throw new ArgumentNullException("ArchiveHelper must not be null!"); ;
			this.filesystemHelper = filesystemHelper ?? throw new ArgumentNullException("FilesystemHelper must not be null!");
			this.processHelper = processHelper ?? throw new ArgumentNullException("ProcessHelper must not be null!");
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
			new UnwindAnalysis(filesystemHelper, coredump, analysisResult).DebugAndSetResultFields();
			Console.WriteLine("Resolving debug symbols ...");
			new DebugSymbolAnalysis(filesystemHelper, processHelper, coredump, analysisResult).DebugAndSetResultFields();
			Console.WriteLine("Setting tags ...");
			new TagAnalyzer(analysisResult).Analyze();
			Console.WriteLine("Setting default fields ...");
			new DefaultFieldsSetter(analysisResult).SetResultFields();
			File.WriteAllText(outputFile, analysisResult.SerializeToJSON());
			Console.WriteLine("Finished coredump analysis.");
		}

		private String GetCoreDumpFilePath(string inputFile) {
			string directory = filesystemHelper.GetParentDirectory(inputFile);
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
				foreach (String file in filesystemHelper.FilesInDirectory(directory)) {
					workDone |= archiveHelper.TryExtract(file);
				}
			}
		}

		private String FindCoredumpOrNull(String directory) {
			foreach (String file in filesystemHelper.FilesInDirectory(directory)) {
				if (file.EndsWith(".core")) {
					return file;
				}
			}
			return null;
		}
	}
}
