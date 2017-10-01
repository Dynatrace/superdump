using SuperDump.Analyzer.Linux.Boundary;
using SuperDump.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Thinktecture.IO;

namespace SuperDump.Analyzer.Linux.Analysis {
	public class GdbAnalyzer {
		public static string GDB_ERR_FILE = "gdb.err.log";
		public static string GDB_OUT_FILE = "gdb.out.log";

		private readonly IFileInfo coredump;
		private readonly SDResult analysisResult;
		private readonly IFilesystem filesystem;
		private readonly IProcessHandler processHandler;

		public GdbAnalyzer(IFilesystem filesystem, IProcessHandler processHandler, IFileInfo coredump, SDResult result) {
			this.filesystem = filesystem ?? throw new ArgumentNullException("FilesystemHelper must not be null!");
			this.processHandler = processHandler ?? throw new ArgumentNullException("ProcessHandler must not be null!");
			this.coredump = coredump ?? throw new ArgumentNullException("Coredump must not be null!");
			this.analysisResult = result ?? throw new ArgumentNullException("SD Result Path must not be null!");
		}

		public void Analyze() {
			try {
				using (ProcessStreams stream = processHandler.StartProcessAndReadWrite("gdb", "")) {
					Console.WriteLine("Starting gdb");
					Task<string> outReader = stream.Output.ReadToEndAsync();
					Task<string> errReader = stream.Error.ReadToEndAsync();
					SendCommandsToGdb(stream.Input);
					if(!outReader.Wait(TimeSpan.FromMinutes(2))) {
						// 2 minutes is very long for gdb (usually finishes in a few seconds even for dumps with >>100 threads)
						Console.WriteLine("GDB parsing timed out! Skipping GDB analysis.");
						return;
					}
					string output = outReader.Result;
					errReader.Wait(TimeSpan.FromSeconds(5));
					string error = errReader.Result;
					Console.WriteLine("Parsing gdb output");
					AnalyzeGdbOutput(output, error);
					Console.WriteLine("Finished gdb parsing");
				}
			} catch (ProcessStartFailedException e) {
				Console.WriteLine($"Failed to start GDB! Skipping GDB analysis: {e.ToString()}");
				return;
			}
		}

		private void SendCommandsToGdb(StreamWriter input) {
			input.WriteLine("set solib-absolute-prefix .");	// load all libraries from the current directory
															// this is especially important because gdb unwinding must match libunwind
			string mainExecutable = ((SDCDSystemContext)analysisResult.SystemContext).FileName;
			if (mainExecutable != null) {
				input.WriteLine("file " + mainExecutable);
			}
			input.WriteLine("core-file " + coredump.FullName);

			foreach (var thread in this.analysisResult.ThreadInformation) {
				input.WriteLine("echo >>thread " + thread.Key + "\\n");
				input.WriteLine("thread " + (thread.Key+1));
				for (int i = 0; i < thread.Value.StackTrace.Count; i++) {
					input.WriteLine("echo >>select " + i + "\\n");
					input.WriteLine("select " + i);
					input.WriteLine("echo >>info args\\n");
					input.WriteLine("info args");
					input.WriteLine("echo >>info locals\\n");
					input.WriteLine("info locals");
					input.WriteLine("echo >>finish frame\\n");
				}
				input.WriteLine("echo >>finish thread\\n");
			}
			input.WriteLine("q");
		}

		private void AnalyzeGdbOutput(string gdbOut, string gdbErr) {
			if (gdbOut.Length > 0) {
				filesystem.WriteToFile(GDB_OUT_FILE, gdbOut);
			}

			if (gdbErr.Length > 0) {
				filesystem.WriteToFile(GDB_ERR_FILE, gdbErr.Trim());
			}
			new GdbOutputParser(analysisResult).Parse(gdbOut);
		}
	}
}
