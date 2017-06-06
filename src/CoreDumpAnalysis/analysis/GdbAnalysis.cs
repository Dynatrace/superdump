using CoreDumpAnalysis.boundary;
using SuperDump.Models;
using System;
using System.Diagnostics;
using System.IO;

namespace CoreDumpAnalysis {
	public class GdbAnalysis {
		public static string GDB_ERR_FILE = "gdb.err.log";
		public static string GDB_OUT_FILE = "gdb.out.log";

		private readonly String coredump;
		private readonly SDResult analysisResult;
		private readonly IFilesystem filesystem;
		private readonly IProcessHandler processHandler;

		public GdbAnalysis(IFilesystem filesystem, IProcessHandler processHandler, String coredump, SDResult result) {
			this.filesystem = filesystem ?? throw new ArgumentNullException("FilesystemHelper must not be null!");
			this.processHandler = processHandler ?? throw new ArgumentNullException("ProcessHandler must not be null!");
			this.coredump = coredump ?? throw new ArgumentNullException("SD Result must not be null!");
			this.analysisResult = result ?? throw new ArgumentNullException("Coredump Path must not be null!");
		}

		public void DebugAndSetResultFields() {
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
			string mainExecutable = FindMainExecutable();
			if (mainExecutable != null) {
				input.WriteLine("file " + mainExecutable);
			}
			input.WriteLine("core-file " + coredump);

			foreach (var thread in this.analysisResult.ThreadInformation) {
				input.WriteLine("echo >>thread " + thread.Key + "\\n");
				input.WriteLine("thread " + thread.Key);
				for (int i = 0; i < thread.Value.StackTrace.Count; i++) {
					input.WriteLine("echo >>select " + i + "\\n");
					input.WriteLine("select " + (i+1));
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
			Console.WriteLine("finished reading output log");
			new GdbOutputParser(analysisResult).Parse(gdbOut);
		}

		private string FindMainExecutable() {
			string exec = ExecutableFromCoredump();
			Console.WriteLine("Executable: " + exec);
			if(exec != null && filesystem.FileExists("." + exec)) {
				return "." + exec;
			}
			return exec;
		}

		private string ExecutableFromCoredump() {
			string execWithArgs = (analysisResult.SystemContext as SDCDSystemContext).Args;
			int firstSpace = execWithArgs?.IndexOf(' ') ?? -1;
			if (firstSpace >= 0) {
				return execWithArgs.Substring(0, firstSpace);
			}
			return execWithArgs;
		}
	}
}
