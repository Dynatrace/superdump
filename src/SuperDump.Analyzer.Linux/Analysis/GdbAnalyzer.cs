using SuperDump.Analyzer.Linux.Boundary;
using SuperDump.Models;
using System;
using System.Diagnostics;
using System.IO;
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
					WorkWithGdb(stream.Input);
					Console.WriteLine("Parsing gdb output");
					AnalyzeGdbOutput(stream.Output, stream.Error);
					Console.WriteLine("Finished gdb parsing");
				}
			} catch (ProcessStartFailedException e) {
				Console.WriteLine("Failed to start GDB: " + e.GetType().Name + " (" + e.Message + "). Skipping GDB analysis.");
				return;
			}
		}

		private void WorkWithGdb(StreamWriter input) {
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

		private void AnalyzeGdbOutput(StreamReader output, StreamReader error) {
			string gdbOut = "";
			while (!output.EndOfStream) {
				gdbOut += output.ReadLine() + Environment.NewLine;
			}
			if (gdbOut.Length > 0) {
				filesystem.WriteToFile(GDB_OUT_FILE, gdbOut);
			}

			string gdbErr = "";
			while (!error.EndOfStream) {
				gdbErr += error.ReadLine() + Environment.NewLine;
			}
			if (gdbErr.Length > 0) {
				filesystem.WriteToFile(GDB_ERR_FILE, gdbErr.Trim());
			}
			new GdbOutputParser(analysisResult).Parse(gdbOut);
		}
	}
}
