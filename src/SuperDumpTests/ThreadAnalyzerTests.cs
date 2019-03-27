using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SuperDump;
using Microsoft.Diagnostics.Runtime;
using System.IO;
using SuperDump.Printers;
using SuperDump.Analyzers;
using SuperDump.Models;
using System.Collections.Generic;

namespace SuperDumpTests {
	[TestClass]
	public class ThreadAnalyzerTests {
		public static string SYMBOL_PATH = Environment.GetEnvironmentVariable("_NT_SYMBOL_PATH");
		private DumpContext context;

		private IList<string> ReadTraceFromThread932() {
			string fileName = Environment.CurrentDirectory + @"\..\..\..\SuperDump\dumps\dotnetworld2\trace932.txt";
			IList<string> trace = new List<string>();
			foreach (var line in File.ReadLines(fileName)) {
				trace.Add(line);
			}
			return trace;
		}

		[TestInitialize]
		public void Initialize() {
			if (context == null) {
				context = new DumpContext();
				string dump = Environment.CurrentDirectory + @"\..\..\..\SuperDump\dumps\dotnetworld2\dotnetworld2.dmp";
				Assert.IsTrue(File.Exists(dump));
				string dac = null;
				DataTarget target = DataTarget.LoadCrashDump(dump, CrashDumpReader.ClrMD);
				target.SymbolLocator.SymbolPath = SYMBOL_PATH;
				context.Target = target;
				context.Runtime = target.CreateRuntime(ref dac);
				context.Heap = context.Runtime.Heap;
				context.DumpFile = dump;
				context.DumpDirectory = Path.GetDirectoryName(context.DumpFile);
				context.Printer = new ConsolePrinter();
				context.SymbolLocator = target.SymbolLocator;
				context.SymbolLocator.SymbolPath = SYMBOL_PATH + ";" + context.DumpDirectory;
				context.SymbolPath = SYMBOL_PATH + ";" + context.DumpDirectory;
			}
		}

		[TestCleanup]
		public void Cleanup() {
			if (context != null) {
				context.Dispose();
				context = null;
			}
		}

		[TestMethod]
		public void ThreadAnalyzerInitTest() {
			ThreadAnalyzer analyzer = new ThreadAnalyzer(context);

			Assert.IsNotNull(analyzer.threads);

			foreach (var thread in analyzer.threads.Values) {
				Assert.IsNotNull(thread);
			}
		}

		[TestMethod]
		public void ThreadAnalyzerStacktraceTest() {
			ThreadAnalyzer analyzer = new ThreadAnalyzer(context);

			// pick any thread stacktrace, in this case take thread 932, os id = 27484
			SDThread thread = analyzer.threads[27484];
			Assert.IsNotNull(thread);

			IList<string> trace = ReadTraceFromThread932();

			Assert.AreEqual(trace.Count, thread.StackTrace.Count);

			for (int i = 0; i < trace.Count; i++) {
				var values = trace[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (values.Length > 0) {
					string method = values[2]; // method name after SP and IP
					StringAssert.Equals(thread.StackTrace[i].MethodName, method);
				}
			}
		}
	}
}
