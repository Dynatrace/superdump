using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime;
using SuperDump.Models;
using System.IO;
using SuperDump.Analyzers;
using SuperDump.Printers;
using System.Runtime.ExceptionServices;
using CommandLine;
using Dynatrace.OneAgent.Sdk.Api;

namespace SuperDump {
	public static class Program {
		private static IOneAgentSdk dynatraceSdk = OneAgentSdkFactory.CreateInstance();

		public static string DUMP_LOC;
		private static string OUTPUT_LOC;
		public static string SYMBOL_PATH = Environment.GetEnvironmentVariable("_NT_SYMBOL_PATH");

		private static int Main(string[] args) {
			try {
				var result = Parser.Default.ParseArguments<Options>(args)
					.WithParsed(options => {
						var tracer = dynatraceSdk.TraceIncomingRemoteCall("AnalyzeWindows", "SuperDump.exe", "SuperDump.exe");
						tracer.SetDynatraceStringTag(options.TraceTag);
						tracer.Trace(() => RunAnalysis(options));
					});
			} catch (AggregateException ae) {
				Console.Error.WriteLine("AggregateException happened:");
				foreach (var e in ae.Flatten().InnerExceptions) {
					Console.Error.WriteLine($"inner exception: {e}");
				}
				return 1;
			} catch (Exception e) {
				Console.Error.WriteLine($"Exception happened: {e}");
				return 1;
			}
			return 0;
		}

		private static void RunAnalysis(Options options) {
			if (Environment.Is64BitProcess) {
				Environment.SetEnvironmentVariable("_NT_DEBUGGER_EXTENSION_PATH", @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\WINXP;C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\winext;C:\Program Files (x86)\Windows Kits\10\Debuggers\x64;");
			} else {
				Environment.SetEnvironmentVariable("_NT_DEBUGGER_EXTENSION_PATH", @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x86\WINXP;C:\Program Files (x86)\Windows Kits\10\Debuggers\x86\winext;C:\Program Files (x86)\Windows Kits\10\Debuggers\x86;");
			}
			using (var context = new DumpContext()) {
				Console.WriteLine("SuperDump - Windows dump analysis tool");
				Console.WriteLine("--------------------------");
				//check if symbol path is set
				if (string.IsNullOrEmpty(SYMBOL_PATH)) {
					Console.WriteLine("WARNING: Environment variable _NT_SYMBOL_PATH is not set!");
				}

				DUMP_LOC = options.DumpFile;
				OUTPUT_LOC = options.OutputFile;

				string absoluteDumpFile = Path.GetFullPath(DUMP_LOC);
				Console.WriteLine(absoluteDumpFile);

				var logfile = new FileInfo(Path.Combine(Path.GetDirectoryName(OUTPUT_LOC), "superdump.log"));
				context.Printer = new FilePrinter(logfile.FullName);

				if (File.Exists(absoluteDumpFile)) {
					LoadDump(context, absoluteDumpFile);

					// do this as early as possible, as some WinDbg commands help us get the right DAC files
					RunSafe(context, nameof(WinDbgAnalyzer), () => {
						var windbgAnalyzer = new WinDbgAnalyzer(context, Path.Combine(context.DumpDirectory, "windbg.log"));
						windbgAnalyzer.Analyze();
					});

					// start analysis
					var analysisResult = new SDResult();
					analysisResult.IsManagedProcess = context.Target.ClrVersions.Count > 0;

					if (analysisResult.IsManagedProcess) {
						SetupCLRRuntime(context);
					}

					RunSafe(context, nameof(ExceptionAnalyzer), () => {
						var sysInfo = new SystemAnalyzer(context);
						analysisResult.SystemContext = sysInfo.systemInfo;

						//get non loaded symbols
						List<string> notLoadedSymbols = new List<string>();
						foreach (var item in sysInfo.systemInfo.Modules) {
							if (item.PdbInfo == null || string.IsNullOrEmpty(item.PdbInfo.FileName) || string.IsNullOrEmpty(item.PdbInfo.Guid)) {
								notLoadedSymbols.Add(item.FileName);
							}
							analysisResult.NotLoadedSymbols = notLoadedSymbols;
						}

						// print to log
						sysInfo.PrintArchitecture();
						sysInfo.PrintCLRVersions();
						sysInfo.PrintAppDomains();
						sysInfo.PrintModuleList();
					});

					RunSafe(context, nameof(ExceptionAnalyzer), () => {
						var exceptionAnalyzer = new ExceptionAnalyzer(context, analysisResult);
					});

					RunSafe(context, nameof(ThreadAnalyzer), () => {
						context.WriteInfo("--- Thread analysis ---");
						ThreadAnalyzer threadAnalyzer = new ThreadAnalyzer(context);
						analysisResult.ExceptionRecord = threadAnalyzer.exceptions;
						analysisResult.ThreadInformation = threadAnalyzer.threads;
						analysisResult.DeadlockInformation = threadAnalyzer.deadlocks;
						analysisResult.LastExecutedThread = threadAnalyzer.GetLastExecutedThreadOSId();
						context.WriteInfo("Last executed thread (engine id): " + threadAnalyzer.GetLastExecutedThreadEngineId().ToString());

						threadAnalyzer.PrintManagedExceptions();
						threadAnalyzer.PrintCompleteStackTrace();
					});

					RunSafe(context, nameof(MemoryAnalyzer), () => {
						var memoryAnalyzer = new MemoryAnalyzer(context);
						analysisResult.MemoryInformation = memoryAnalyzer.memDict;
						analysisResult.BlockingObjects = memoryAnalyzer.blockingObjects;

						memoryAnalyzer.PrintExceptionsObjects();
					});

					// this analyzer runs after all others to put tags onto taggableitems
					RunSafe(context, nameof(TagAnalyzer), () => {
						var tagAnalyzer = new TagAnalyzer(analysisResult);
						tagAnalyzer.Analyze();
					});

					// write to json
					analysisResult.WriteResultToJSONFile(OUTPUT_LOC);

					context.WriteInfo("--- End of output ---");
					Console.WriteLine("done.");
				} else {
					throw new FileNotFoundException("File can not be found!");
				}
			}
		}

		private static void LoadDump(DumpContext context, string absoluteDumpFile) {
			try {
				var target = DataTarget.LoadCrashDump(absoluteDumpFile, CrashDumpReader.ClrMD);

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
				throw;
			} catch (Exception ex) {
				context.WriteError("An exception occured while loading crash dump.");
				context.WriteError(ex.Message);
				context.WriteLine(ex.StackTrace);
				throw;
			}
		}

		private static void SetupCLRRuntime(DumpContext context) {
			// for .NET specific
			try {
				string dac = null;
				context.DacLocation = dac;
				context.Runtime = context.Target.CreateRuntime(ref dac);
				context.WriteInfo("created runtime with version " + context.Runtime.ClrInfo.Version);
				context.Heap = context.Runtime.Heap;
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

		/// <summary>
		/// Executes the action, catches any exception and logs it
		/// </summary>
		[HandleProcessCorruptedStateExceptions] // some operations out of our control (WinDbg ExecuteCommand) cause AccessViolationExceptions in some cases. Be brave and try to continute to run. This attribute allows an exception-handler to catch it.
		private static void RunSafe(DumpContext context, string name, Action action) {
			try {
				action();
			} catch (Exception e) {
				context.WriteError($"WinDbgAnalyzer failed: {e}");
			}
		}
	}
}
