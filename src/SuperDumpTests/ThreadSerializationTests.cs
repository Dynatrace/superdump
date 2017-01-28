using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Diagnostics.Runtime;
using SuperDump;
using SuperDump.Printers;
using System.IO;
using SuperDump.Analyzers;
using SuperDump.Models;
using Newtonsoft.Json;
using System.Linq;

namespace SuperDumpTests {
	[TestClass]
	public class ThreadSerializationTests {
		public static string SYMBOL_PATH = Environment.GetEnvironmentVariable("_NT_SYMBOL_PATH");
		private DumpContext context;

		[TestInitialize]
		public void Initialize() {
			if (context == null) {
				context = new DumpContext();
				string dump = Environment.CurrentDirectory + @"\..\..\..\SuperDump\dumps\DeadlockSample\DeadlockSample.dmp";
				Assert.IsTrue(File.Exists(dump));
				string dac = null;
				DataTarget target = DataTarget.LoadCrashDump(dump, CrashDumpReader.ClrMD);

				context.Target = target;
				context.Runtime = target.CreateRuntime(ref dac);
				context.Heap = context.Runtime.GetHeap();
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
		public void ThreadSerializationTest() {
			ThreadAnalyzer analyzer = new ThreadAnalyzer(context);
			Assert.IsNotNull(analyzer.threads);

			foreach (var key in analyzer.threads.Keys) {
				SDThread t = analyzer.threads[key];
				Assert.AreEqual(t.OsId, key); // should be the same, as threads are inserted in the dictionary with their OS id

				string json = t.StackTrace.SerializeToJSON();
				SDCombinedStackTrace trace = JsonConvert.DeserializeObject<SDCombinedStackTrace>(json);

				Assert.IsNotNull(trace);
				Assert.IsTrue(Enumerable.SequenceEqual(t.StackTrace, trace));
			}
		}

		[TestMethod]
		public void ThreadAnalyzerDeadlockSerializationTest() {
			ThreadAnalyzer analyzer = new ThreadAnalyzer(context);

			foreach (var deadlock in analyzer.deadlocks) {
				string json = JsonConvert.SerializeObject(deadlock, Formatting.Indented, new JsonSerializerSettings {
					ReferenceLoopHandling = ReferenceLoopHandling.Ignore
				});

				Assert.IsNotNull(json);

				SDDeadlockContext deadlockAfter = JsonConvert.DeserializeObject<SDDeadlockContext>(json);

				Assert.AreEqual(deadlock, deadlockAfter);

				string json2 = JsonConvert.SerializeObject(deadlockAfter, Formatting.Indented, new JsonSerializerSettings {
					ReferenceLoopHandling = ReferenceLoopHandling.Ignore
				});

				StringAssert.Equals(json, json2);
			}
		}
	}
}
