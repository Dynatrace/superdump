using SuperDump;
using System;
using Microsoft.Diagnostics.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace SuperDumpTests {
	[TestClass]
	public class LoadingDumpTests {
		public static string SYMBOL_PATH = Environment.GetEnvironmentVariable("_NT_SYMBOL_PATH");
		public static string sample = Path.Combine(Environment.CurrentDirectory, "..\\..\\..\\SuperDump\\dumps\\DeadlockSample\\DeadlockSample.dmp");

		[TestMethod]
		public void CheckTestDirExists() {
			string dir = Environment.CurrentDirectory + @"\..\..\..\SuperDump\dumps";

			Assert.IsTrue(Directory.Exists(dir));
		}

		[TestMethod]
		public void CanLoadSampleDumpClrMD() {
			Assert.IsTrue(File.Exists(sample));

			DataTarget target = DataTarget.LoadCrashDump(sample, CrashDumpReader.ClrMD);
			Assert.IsNotNull(target);
		}

		[TestMethod]
		public void CanLoadSampleDumpDbgEng() {
			Assert.IsTrue(File.Exists(sample));

			DataTarget target = DataTarget.LoadCrashDump(sample, CrashDumpReader.DbgEng);
			Assert.IsNotNull(target);
		}

		[TestMethod]
		public void CreateRuntimeTest() {
			Assert.IsTrue(File.Exists(sample));

			DataTarget target = DataTarget.LoadCrashDump(sample, CrashDumpReader.ClrMD);
			target.SymbolLocator.SymbolPath = SYMBOL_PATH;
			Assert.IsNotNull(target);
			string dac = null;
			ClrRuntime runtime = target.CreateRuntime(ref dac);
			Assert.IsNotNull(dac);
			System.Diagnostics.Debug.WriteLine(dac);
		}
	}
}
