using SuperDump.Models;
using System;
using System.IO;
using Thinktecture.IO;
using System.Linq;
using SuperDump.Analyzer.Linux.Boundary;
using System.Threading.Tasks;
using SuperDump.Analyzer.Common;

namespace SuperDump.Analyzer.Linux.Analysis {
	public class CoreDumpAnalyzer {
		private readonly IArchiveHandler archiveHandler;
		private readonly IFilesystem filesystem;
		private readonly IProcessHandler processHandler;
		private readonly IHttpRequestHandler requestHandler;

		public CoreDumpAnalyzer(IArchiveHandler archiveHandler, IFilesystem filesystem, IProcessHandler processHandler, IHttpRequestHandler requestHandler) {
			this.archiveHandler = archiveHandler ?? throw new ArgumentNullException("ArchiveHandler must not be null!");
			this.filesystem = filesystem ?? throw new ArgumentNullException("Filesystem must not be null!");
			this.processHandler = processHandler ?? throw new ArgumentNullException("ProcessHandler must not be null!");
			this.requestHandler = requestHandler ?? throw new ArgumentNullException("RequestHandler must not be null!");
		}

		public async Task AnalyzeAsync(string inputFile, string outputFile) {
			IFileInfo coredump = GetCoreDumpFilePath(inputFile);
			if (coredump == null) {
				Console.WriteLine("No core dump found.");
				// TODO write empty json?
				return;
			}
			Console.WriteLine($"Processing core dump file: {coredump}");

			SDResult analysisResult = new SDResult();
			new UnwindAnalyzer(coredump, analysisResult).Analyze();
			Console.WriteLine("Retrieving shared libraries ...");
			await new SharedLibAnalyzer(filesystem, coredump, analysisResult).AnalyzeAsync();
			Console.WriteLine("Finding executable file ...");
			new ExecutablePathAnalyzer(filesystem, analysisResult.SystemContext as SDCDSystemContext).Analyze();
			Console.WriteLine("Retrieving agent version if available ...");
			new CoreLogAnalyzer(filesystem, coredump, analysisResult.SystemContext.Modules).Analyze();
			Console.WriteLine("Fetching debug symbols ...");
			new DebugSymbolResolver(filesystem, requestHandler).Resolve(analysisResult.SystemContext.Modules);
			Console.WriteLine("Resolving debug symbols ...");
			new DebugSymbolAnalysis(filesystem, processHandler, analysisResult).Analyze();
			Console.WriteLine("Reading stack information ...");
			new GdbAnalyzer(filesystem, processHandler, coredump, analysisResult).Analyze();
			Console.WriteLine("Setting default fields ...");
			new DefaultFieldsSetter(analysisResult).SetResultFields();
			Console.WriteLine("Setting tags ...");
			new DynamicAnalysisBuilder(analysisResult,
			   new UniversalTagAnalyzer(), new LinuxTagAnalyzer(), new DotNetTagAnalyzer(), new DynatraceTagAnalyzer())
			   .Analyze();
			File.WriteAllText(outputFile, analysisResult.SerializeToJSON());
			Console.WriteLine("Finished coredump analysis.");
		}

		private IFileInfo GetCoreDumpFilePath(string inputFile) {
			IFileInfo file = filesystem.GetFile(inputFile);
			if(file.Exists) {
				if(file.Extension == ".core") {
					return file;
				} else if (file.Extension == ".tar" || file.Extension == ".gz" || file.Extension == ".tgz" || file.Extension == ".tar" || file.Extension == ".zip") {
					IDirectoryInfo directory = file.Directory;
					Console.WriteLine($"Extracting archives in directory {directory.FullName}");
					ExtractArchivesInDir(directory);
					return FindCoredumpOrNull(directory);
				} else {
					Console.WriteLine($"Could not identify input file {inputFile}. Assuming it is a core dump.");
					return file;
				}
			} else {
				IDirectoryInfo directory = filesystem.GetDirectory(inputFile);
				if(directory.Exists) {
					Console.WriteLine($"Extracting archives in directory {directory.FullName}");
					ExtractArchivesInDir(directory);
					return FindCoredumpOrNull(directory);
				} else {
					Console.WriteLine("Input file does not exist!");
					return null;
				}
			}
		}

		private void ExtractArchivesInDir(IDirectoryInfo directory) {
			bool workDone = true;
			while (workDone) {
				workDone = directory.EnumerateFiles("*", SearchOption.AllDirectories)
					.Select(fi => archiveHandler.TryExtractAndDelete(fi))
					.Any(extracted => extracted == true);
			}
		}

		private IFileInfo FindCoredumpOrNull(IDirectoryInfo directory) {
			return directory.EnumerateFiles("*.core", SearchOption.AllDirectories).FirstOrDefault();
		}
	}
}
