using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SuperDump;
using SuperDump.Models;
using Microsoft.Diagnostics.Runtime;
using SuperDump.Printers;
using SuperDump.Analyzers;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace SuperDumpTests {

	public static class StringExtensions {
		public static string RemoveWhitespaces(this string s) {
			return new string(s.Where(c => !Char.IsWhiteSpace(c)).ToArray());
		}
	}
	/// <summary>
	/// Test class for system analyzer, mostly completely scripted,
	/// sample dump DeadlockSample.dmp was used, reference values were taken from WinDbg
	/// </summary>
	[TestClass]
	public class SystemAnalyzerTests {
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
				context.Heap = context.Runtime.Heap;
				context.DumpFile = dump;
				context.DumpDirectory = Path.GetDirectoryName(context.DumpFile);
				context.Printer = new ConsolePrinter();
				context.SymbolLocator = target.SymbolLocator;
				context.SymbolPath = target.SymbolLocator.SymbolPath;
			}
		}

		[TestCleanup]
		public void Cleanup() {
			if (context != null) {
				context.Dispose();
				context = null;
			}
		}

		private string ReadOSString() {
			string fileName = Environment.CurrentDirectory + @"\..\..\..\SuperDump\dumps\dotnetworld2\header.txt";

			string osVersion = null;
			foreach (var line in File.ReadLines(fileName)) {
				var values = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

				if (values.Length > 1 && values[0].Equals("Built") && values[1].Equals("by:")) {
					StringBuilder sb = new StringBuilder();
					for (int i = 2; i < values.Length; i++) {
						sb.Append(values[i]);
						sb.Append(' ');
					}
					sb.Remove(sb.Length - 1, 1);
					osVersion = sb.ToString();
				}
			}
			return osVersion;
		}

		private IList<string> ReadDomains() {
			string fileName = Environment.CurrentDirectory + @"\..\..\..\SuperDump\dumps\dotnetworld2\domains.txt";
			List<string> domainNames = new List<string>();
			foreach (var line in File.ReadLines(fileName)) {
				var values = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				// get name line
				if (values.Length > 0 && values[0].Equals("Name:")) {
					// this is the line where the app domain name is found
					StringBuilder sb = new StringBuilder();
					for (int i = 1; i < values.Length; i++) {
						sb.Append(values[i]);
					}
					domainNames.Add(sb.ToString());
				}
			}
			return domainNames;
		}

		private struct ModuleVersion {
			public string name;
			public string version;
		}

		private IList<ModuleVersion> ReadModules() {
			string fileName = Environment.CurrentDirectory + @"\..\..\..\SuperDump\dumps\dotnetworld2\modules.txt";
			List<ModuleVersion> versions = new List<ModuleVersion>();
			StreamReader reader = new StreamReader(fileName);

			while (reader.Peek() >= 0) {
				var line = reader.ReadLine();
				var values = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

				if (values.Length > 1 && values[0].Equals("Image") && values[1].Equals("name:")) {
					ModuleVersion v = new ModuleVersion();
					v.name = values[2];
					// go to version
					while (reader.Peek() >= 0) {
						var line2 = reader.ReadLine();
						var values2 = line2.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
						if (values2.Length > 1 && values2[0].Equals("File") && values2[1].Equals("version:")) {
							v.version = values2[2];
							versions.Add(v);
							break;
						}
					}
				}
			}
			return versions;
		}

		[TestMethod]
		public void SystemAnalyzerAppDomainTest() {
			SystemAnalyzer analyzer = new SystemAnalyzer(context);
			IList<string> domainNames = ReadDomains();

			// Should include one AppDomain with name "dotNetWorld4.5.exe"
			Assert.IsNotNull(analyzer.systemInfo.AppDomains.First(domain => domain.Name.Equals("dotNetWorld4.5.exe")));

			// in this dump there should be exactly 15 app domains according to WinDbg
			Assert.AreEqual(domainNames.Count(n => n.ToLower() != "none"), analyzer.systemInfo.AppDomains.Count);

			// assert on system domain and shared domain
			Assert.IsNotNull(analyzer.systemInfo.SystemDomain);
			Assert.IsNotNull(analyzer.systemInfo.SharedDomain);
			// assert on right addresses
			Assert.AreEqual((ulong)1932093040, analyzer.systemInfo.SharedDomain.Address);
			Assert.AreEqual((ulong)1932093888, analyzer.systemInfo.SystemDomain.Address);

			// walk through "normal" app domains
			foreach (var domain in domainNames.Where(name => name.ToLower() != "none")) {
				// assert that for every domain in windbg log, 
				// a corresponding app domain can be found in the analyzer.systemInfo.AppDomains collection
				Assert.IsNotNull(analyzer.systemInfo.AppDomains
					.FirstOrDefault(d => d.Name.ToLower()
											.RemoveWhitespaces()
											.Equals(domain.ToLower())));
			}
		}

		[TestMethod]
		public void SystemAnalyzerModulesTest() {
			SystemAnalyzer analyzer = new SystemAnalyzer(context);
			Assert.IsNotNull(analyzer.systemInfo.Modules);

			// parse modules from windbg lm command
			IList<ModuleVersion> winDbgModules = ReadModules();

			// according to windbg, there must be 58 modules
			Assert.AreEqual(winDbgModules.Count, analyzer.systemInfo.Modules.Count);

			// replace special characters and underscores
			foreach (SDModule module in analyzer.systemInfo.Modules) {
				module.FileName = Regex.Replace(module.FileName, @"[^a-zA-Z0-9]", "");
			}

			foreach (ModuleVersion module in winDbgModules) {
				string moduleWithoutSpecialChars = Regex.Replace(module.name, @"[^a-zA-Z0-9]", "");

				Assert.IsNotNull(analyzer.systemInfo.Modules
					.FirstOrDefault(m => m.FileName.Contains(moduleWithoutSpecialChars)));
			}
		}

		[TestMethod]
		public void SystemAnalyzerClrVersionsTest() {
			SystemAnalyzer analyzer = new SystemAnalyzer(context);
			Assert.IsNotNull(analyzer.systemInfo.Modules);

			// get mscorwks or clr out of modules, these are the interesting dlls
			IList<ModuleVersion> winDbgClrModules = ReadModules()
														.Where(m => m.name.ToLower().Equals("clr.dll")
																|| m.name.ToLower().Equals("mscorwks.dll"))
														.ToList();
			Assert.AreEqual(winDbgClrModules.Count, analyzer.systemInfo.ClrVersions.Count);

			foreach (var module in winDbgClrModules) {
				Assert.IsNotNull(analyzer.systemInfo.ClrVersions.FirstOrDefault(clr => clr.Version.Contains(module.version)));
			}
		}

		[TestMethod]
		public void SystemAnalyzerOSVersionTest() {
			SystemAnalyzer analyzer = new SystemAnalyzer(context);
			Assert.IsNotNull(analyzer.systemInfo.OSVersion);

			string osVersion = ReadOSString();
			Assert.IsNotNull(osVersion);

			StringAssert.Contains(analyzer.systemInfo.OSVersion, osVersion);
		}
	}
}
