using SuperDump;
using SuperDump.Analyzers;
using SuperDump.Models;
using SuperDump.Printers;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Diagnostics.Runtime;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace SuperDumpTests {
	[TestClass]
	public class SystemContextSerializationTests {
		public static string SYMBOL_PATH = Environment.GetEnvironmentVariable("_NT_SYMBOL_PATH");
		private DumpContext context;

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
		public void SystemContextSerializationTest() {
			var analyzer = new SystemAnalyzer(this.context);
			Assert.IsNotNull(analyzer);

			string json = analyzer.SerializeSystemInfoToJSON();
			// deserialize
			SDSystemContext systemInfo = JsonConvert.DeserializeObject<SDSystemContext>(json);

			// serialize again, and then check strings
			string json2 = systemInfo.SerializeToJSON();

			// check if objects are equal, relies on correct equals implementation!
			Assert.AreEqual(analyzer.systemInfo, systemInfo);

			// check json before serializing and after deserializing
			StringAssert.Equals(json, json2);
		}

		[TestMethod]
		public void AppDomainSerializationTest() {
			SystemAnalyzer analyzer = new SystemAnalyzer(this.context);
			List<SDAppDomain> domains = new List<SDAppDomain>();
			foreach (SDAppDomain domainBefore in analyzer.systemInfo.AppDomains) {
				string json = domainBefore.SerializeToJSON();
				// deserialize domain
				SDAppDomain domainAfter = JsonConvert.DeserializeObject<SDAppDomain>(json);
				domains.Add(domainAfter);
				// assert
				Assert.AreEqual(domainBefore, domainAfter);

				// serialize again and check
				string jsonAfter = domainAfter.SerializeToJSON();
				StringAssert.Equals(json, jsonAfter);
			}

			// extra assert on collection, assume order stays the same after deserialization
			Assert.IsTrue(Enumerable.SequenceEqual(domains, analyzer.systemInfo.AppDomains));
		}

		[TestMethod]
		public void ModuleSerializationTest() {
			SystemAnalyzer analyzer = new SystemAnalyzer(this.context);
			List<SDModule> modules = new List<SDModule>();
			foreach (SDModule moduleBefore in analyzer.systemInfo.Modules) {
				string json = moduleBefore.SerializeToJSON();

				// deserialize module
				SDModule moduleAfter = JsonConvert.DeserializeObject<SDModule>(json);
				modules.Add(moduleAfter);

				// assert
				Assert.AreEqual(moduleBefore, moduleAfter);

				// serialize again and check 
				string jsonAfter = moduleAfter.SerializeToJSON();
				StringAssert.Equals(json, jsonAfter);
			}

			Assert.IsTrue(Enumerable.SequenceEqual(analyzer.systemInfo.Modules, modules));
		}

		[TestMethod]
		public void ClrVersionsSerializationTest() {
			SystemAnalyzer analyzer = new SystemAnalyzer(this.context);
			List<SDClrVersion> versions = new List<SDClrVersion>();
			foreach (SDClrVersion versionBefore in analyzer.systemInfo.ClrVersions) {
				string json = versionBefore.SerializeToJSON();

				SDClrVersion versionAfter = JsonConvert.DeserializeObject<SDClrVersion>(json);
				versions.Add(versionAfter);
				Assert.AreEqual(versionBefore, versionAfter);

				// serialize again and check version
				string jsonAfter = versionAfter.SerializeToJSON();
				StringAssert.Equals(json, jsonAfter);
			}

			Assert.IsTrue(Enumerable.SequenceEqual(analyzer.systemInfo.ClrVersions, versions));
		}
	}
}
