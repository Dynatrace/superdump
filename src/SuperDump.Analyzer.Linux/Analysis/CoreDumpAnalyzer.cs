using SuperDump.Models;
using System;
using System.IO;
using Thinktecture.IO;
using System.Linq;
using SuperDump.Analyzer.Linux.Boundary;
using System.Threading.Tasks;
using SuperDump.Analyzer.Common;
using SuperDump.Common;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using SuperDumpModels;

namespace SuperDump.Analyzer.Linux.Analysis {
	public class CoreDumpAnalyzer {
		[DllImport(Configuration.WRAPPER)]
		private static extern void init(string filepath, string workindDir);
		[DllImport(Configuration.WRAPPER)]
		private static extern void destroy();

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

		/// <summary>
		/// Prepare is only called when starting a GDB interactive mode session.
		/// This method resolves debug symbols and downloads source files if available.
		/// If there is no superdump-result.json file, we also need to analyze shared libraries and unwind stacktraces.
		/// If initializing a GDB session takes long, you may try to disable retrieving source files (see Configuration.cs/SOURCE_REPO_URL)
		/// </summary>
		public IFileInfo Prepare(string inputFile) {
			IFileInfo coredump = GetCoreDumpFile(inputFile);
			if (coredump == null) {
				Console.WriteLine("No core dump found.");
				return null;
			}

			string jsonFile = Path.Combine(coredump.DirectoryName, "superdump-result.json");
			SDResult analysisResult = null;
			bool resultReadFromJson = false;
			if (File.Exists(jsonFile)) {
				string json = File.ReadAllText(jsonFile);
				try {
					analysisResult = JsonConvert.DeserializeObject<SDResult>(json,
						new SDSystemContextConverter(), new SDModuleConverter(), new SDCombinedStackFrameConverter());
					resultReadFromJson = true;
					Console.WriteLine("Successfully desearialized superdump-result.json");
				} catch (Exception e) {
					Console.WriteLine($"Failed to read analysis result for dump {coredump.FullName} ({e.GetType()}): {e.Message}");
				}
			}
			if (!resultReadFromJson) {
				analysisResult = new SDResult();

				analysisResult.SystemContext = new SDCDSystemContext();
				Console.WriteLine("Retrieving shared libraries ...");
				new SharedLibAnalyzer(filesystem, coredump, analysisResult, false).AnalyzeAsync().Wait();
				if ((analysisResult?.SystemContext?.Modules?.Count ?? 0) == 0) {
					Console.WriteLine("Failed to extract shared libraries from core dump.");
					return null;
				}
			}

			Console.WriteLine("Resolving debug symbols ...");
			new DebugSymbolResolver(filesystem, requestHandler, processHandler).Resolve(analysisResult.SystemContext.Modules);

			if(!resultReadFromJson) {
				// stacktraces are required to retrieve source files
				new UnwindAnalyzer(coredump, analysisResult).Analyze();
			}

			// Retrieve source files from repository. This will only work if stacktraces are available, i.e. the superdump-result.json exists and is valid
			// rerunning the unwinding would take too much time. (would it?)
			Console.WriteLine("Retrieving source files ...");
			new SourceFileProvider(analysisResult, filesystem, requestHandler).ProvideSourceFiles(Path.Combine(coredump.DirectoryName, "sources"));

			Console.WriteLine($"Dump preparation finished for coredump: {coredump}");
			return coredump;
		}

		public async Task<LinuxAnalyzerExitCode> AnalyzeAsync(string inputFile, string outputFile) {
			IFileInfo coredump = GetCoreDumpFile(inputFile);
			if (coredump == null) {
				return LinuxAnalyzerExitCode.NoCoredumpFound;
			}
			Console.WriteLine($"Processing core dump file: {coredump}");

			init(coredump.FullName, coredump.Directory.FullName);

			SDResult analysisResult = new SDResult();
			analysisResult.SystemContext = new SDCDSystemContext();

			Console.WriteLine("Retrieving main executable ...");
			new ExecutablePathAnalyzer(filesystem, (SDCDSystemContext)analysisResult.SystemContext).Analyze();
			Console.WriteLine("Retrieving shared libraries ...");
			await new SharedLibAnalyzer(filesystem, coredump, analysisResult, true).AnalyzeAsync();
			if ((analysisResult?.SystemContext?.Modules?.Count ?? 0) == 0) {
				Console.WriteLine("No shared libraries detected. Maybe NT_FILE note is missing? Trying to retrieve libraries via GDB.");
				new GdbSharedLibAnalyzer(filesystem, processHandler, coredump, analysisResult).Analyze();
				if ((analysisResult?.SystemContext?.Modules?.Count ?? 0) == 0) {
					Console.WriteLine("Could not extract libs from GDB either. Terminating because further analysis will not make sense.");
					return LinuxAnalyzerExitCode.NoSharedLibraries;
				}
			} else {
				Console.WriteLine($"Detected {analysisResult.SystemContext.Modules.Count} shared libraries.");
			}
			Console.WriteLine("Resolving debug symbols ...");
			new DebugSymbolResolver(filesystem, requestHandler, processHandler).Resolve(analysisResult.SystemContext.Modules);
			Console.WriteLine("Unwinding stacktraces ...");
			new UnwindAnalyzer(coredump, analysisResult).Analyze();
			Console.WriteLine("Retrieving agent version if available ...");
			new CoreLogAnalyzer(filesystem, coredump, analysisResult.SystemContext.Modules).Analyze();
			Console.WriteLine("Fetching debug symbols ...");
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
			destroy();
			return LinuxAnalyzerExitCode.Success;
		}

		private IFileInfo GetCoreDumpFile(string inputFile) {
			IFileInfo file = filesystem.GetFile(inputFile);
			if (file.Exists) {
				if (file.Extension == ".core") {
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
				if (directory.Exists) {
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
