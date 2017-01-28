using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime;
using SuperDump.Models;
using System.IO;
using SuperDump.Analyzers;
using SuperDump.Printers;

namespace SuperDump {
	public static class Program {
		public static string DUMP_LOC;
		public static string SYMBOL_PATH = Environment.GetEnvironmentVariable("_NT_SYMBOL_PATH");

		private static DumpContext context;
		private static DataTarget target;

		private static void Main(string[] args) {
			using (context = new DumpContext()) {
				Console.WriteLine("SuperDump - Windows dump analysis tool");
				Console.WriteLine("--------------------------");
				//check if symbol path is set
				if (string.IsNullOrEmpty(SYMBOL_PATH)) {
					Console.WriteLine("WARNING: Environment variable _NT_SYMBOL_PATH is not set!");
				}

				if (args.Length < 1) {
					Console.WriteLine("no dump file was specified! Please enter dump path: ");
					DUMP_LOC = Console.ReadLine();
				} else {
					DUMP_LOC = args[0];
				}

				string absoluteDumpFile = Path.GetFullPath(DUMP_LOC);

				Console.WriteLine(absoluteDumpFile);
				context.Printer = new FilePrinter(absoluteDumpFile + ".log");

				try {
					if (File.Exists(absoluteDumpFile)) {
						LoadDump(absoluteDumpFile);

						// do this as early as possible, as some WinDbg commands help us get the right DAC files
						var windbgAnalyzer = new WinDbgAnalyzer(context, Path.Combine(context.DumpDirectory, "windbg.log"));
						windbgAnalyzer.Analyze();

						// start analysis
						var analysisResult = new SDResult();
						analysisResult.IsManagedProcess = context.Target.ClrVersions.Count > 0;

						if (analysisResult.IsManagedProcess) {
							SetupCLRRuntime();
						}

						var sysInfo = new SystemAnalyzer(context);
						analysisResult.SystemContext = sysInfo.systemInfo;

						var exceptionAnalyzer = new ExceptionAnalyzer(context, analysisResult);

						context.WriteInfo("--- Thread analysis ---");
						ThreadAnalyzer threadAnalyzer = new ThreadAnalyzer(context);
						analysisResult.ExceptionRecord = threadAnalyzer.exceptions;
						analysisResult.ThreadInformation = threadAnalyzer.threads;
						analysisResult.DeadlockInformation = threadAnalyzer.deadlocks;
						analysisResult.LastExecutedThread = threadAnalyzer.GetLastExecutedThreadOSId();
						context.WriteInfo("Last executed thread (engine id): " + threadAnalyzer.GetLastExecutedThreadEngineId().ToString());

						var analyzer = new MemoryAnalyzer(context);
						analysisResult.MemoryInformation = analyzer.memDict;
						analysisResult.BlockingObjects = analyzer.blockingObjects;

						// this analyzer runs after all others to put tags onto taggableitems
						var tagAnalyzer = new TagAnalyzer(analysisResult);
						tagAnalyzer.Analyze();

						//get non loaded symbols
						List<string> notLoadedSymbols = new List<string>();
						foreach (var item in sysInfo.systemInfo.Modules) {
							if (item.PdbInfo == null || string.IsNullOrEmpty(item.PdbInfo.FileName) || string.IsNullOrEmpty(item.PdbInfo.Guid)) {
								notLoadedSymbols.Add(item.FileName);
							}
						}
						analysisResult.NotLoadedSymbols = notLoadedSymbols;

						// print to log
						sysInfo.PrintArchitecture();
						sysInfo.PrintCLRVersions();
						sysInfo.PrintAppDomains();
						sysInfo.PrintModuleList();
						threadAnalyzer.PrintManagedExceptions();
						threadAnalyzer.PrintCompleteStackTrace();
						analyzer.PrintExceptionsObjects();

						// write to json
						analysisResult.WriteResultToJSONFile(context.DumpDirectory + "\\" +
							 Path.GetFileNameWithoutExtension(context.DumpFile) + ".json");

						context.WriteInfo("--- End of output ---");
						Console.WriteLine("done.");
					} else {
						throw new FileNotFoundException("File can not be found!");
					}
				} catch (Exception e) {
					context.WriteError($"Exception happened: {e}");
				}
			}
		}

		private static void LoadDump(string absoluteDumpFile) {
			try {
				target = DataTarget.LoadCrashDump(absoluteDumpFile, CrashDumpReader.ClrMD);

				// attention: CLRMD needs symbol path to be ";" separated. srv*url1*url2*url3 won't work.

				Console.WriteLine(SYMBOL_PATH);
				if (!string.IsNullOrEmpty(SYMBOL_PATH)) {
					target.SymbolLocator.SymbolPath = SYMBOL_PATH;
				}

				context.DumpFile = absoluteDumpFile;
				context.DumpDirectory = Path.GetDirectoryName(absoluteDumpFile);
				context.SymbolLocator = target.SymbolLocator;
				context.SymbolLocator.SymbolPath = target.SymbolLocator.SymbolPath + ";" + context.DumpDirectory;
				context.SymbolPath = target.SymbolLocator.SymbolPath + ";" + context.DumpDirectory;
				context.Target = target;
			} catch (InvalidOperationException ex) {
				context.WriteError("wrong architecture");
				context.WriteLine(ex.Message);
				context.Dispose();
				throw;
			} catch (Exception ex) {
				context.WriteError("Wrong architecture (not started with SuperDumpSelector) or CLR could not be loaded");
				context.WriteError(ex.Message);
				context.WriteLine(ex.StackTrace);
				//context.Dispose();
				//throw ex;
			}
		}

		private static void SetupCLRRuntime() {
			// for .NET specific
			try {
				string dac = null;
				context.DacLocation = dac;
				context.Runtime = target.CreateRuntime(ref dac);
				context.WriteInfo("created runtime with version " + context.Runtime.ClrInfo.Version);
				context.Heap = context.Runtime.GetHeap();
			} catch (FileNotFoundException ex) {
				context.WriteError("The right dac file could not be found.");
				context.WriteLine(ex.Message);
				context.WriteLine(ex.StackTrace);

				context.Runtime = null;
				//context.Dispose();
				//throw ex;

			} catch (Exception ex) {
				context.WriteError("Exception creating CLR Runtime");
				context.WriteError(ex.Message);
				context.WriteLine(ex.StackTrace);
			}
		}

		private static string GetRelativePath(string filespec) {
			return GetRelativePath(filespec, Environment.CurrentDirectory);
		}

		private static string GetRelativePath(string filespec, string folder) {
			Uri pathUri = new Uri(filespec);
			// Folders must end in a slash
			if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString())) {
				folder += Path.DirectorySeparatorChar;
			}
			Uri folderUri = new Uri(folder);
			return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
		}
	}
}
