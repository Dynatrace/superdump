using SuperDump.Analyzer.Linux;
using SuperDump.Doubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SuperDump.Models;
using System.Collections.Generic;
using SuperDump.Analyzer.Linux.Analysis;
using System.IO;

namespace SuperDump.Analyzer.Linux.Test {
	[TestClass]
	public class DebugSymbolResolverTest {
		private DebugSymbolResolver resolver;
		private FilesystemDouble filesystem;
		private RequestHandlerDouble requestHandler;

		private List<SDModule> modules;
		private SDCDModule module;

		[TestInitialize]
		public void Init() {
			this.filesystem = new FilesystemDouble();
			this.requestHandler = new RequestHandlerDouble();
			this.resolver = new DebugSymbolResolver(filesystem, requestHandler);

			this.modules = new List<SDModule>();
			this.module = new SDCDModule() {
				LocalPath = $"some{Path.DirectorySeparatorChar}path{Path.DirectorySeparatorChar}somelib.so",
				FileName = "somelib.so",
				FilePath = $"{Path.DirectorySeparatorChar}lib{Path.DirectorySeparatorChar}ruxit{Path.DirectorySeparatorChar}somelib.so"
			};
			this.modules.Add(module);
		}

		[TestMethod]
		public void TestWithoutModules() {
			resolver.Resolve(new List<SDModule>());
			AssertNoRequestsMade();
		}

		[TestMethod]
		public void TestWithoutBinary() {
			module.LocalPath = null;
			resolver.Resolve(this.modules);
			AssertNoRequestsMade();
		}

		[TestMethod]
		public void TestWithIrrelevantBinary() {
			module.FilePath = $"some{Path.DirectorySeparatorChar}path";
			module.FileName = "somelib.so";
			resolver.Resolve(this.modules);
			AssertNoRequestsMade();
		}

		[TestMethod]
		public void TestDebugFilePresent() {
			filesystem.Md5 = "some-md5-hash";
			filesystem.ExistingFiles.Add($"{Constants.DEBUG_SYMBOL_PATH}some-md5-hash{Path.DirectorySeparatorChar}somelib.dbg");
			resolver.Resolve(this.modules);
			Assert.IsTrue(module.DebugSymbolPath.EndsWith($"some-md5-hash{Path.DirectorySeparatorChar}somelib.dbg"), $"Invalid DebugSymbol path: {module.DebugSymbolPath}");
			AssertNoRequestsMade();
		}

		[TestMethod]
		public void TestDownloadDebugSymbolFail() {
			filesystem.Md5 = "some-md5-hash";
			requestHandler.Return = false;
			resolver.Resolve(this.modules);
			Assert.IsNull(module.DebugSymbolPath);
			AssertValidRequestDone();
		}

		[TestMethod]
		public void TestDownloadDebugSymbol() {
			filesystem.Md5 = "some-md5-hash";
			requestHandler.Return = true;
			resolver.Resolve(this.modules);
			Assert.IsTrue(module.DebugSymbolPath.EndsWith($"some-md5-hash{Path.DirectorySeparatorChar}somelib.dbg"), $"Invalid DebugSymbol path: {module.DebugSymbolPath}");
			AssertValidRequestDone();
		}

		private void AssertNoRequestsMade() {
			Assert.IsNull(requestHandler.FromUrl);
			Assert.IsNull(requestHandler.ToFile);
		}

		private void AssertValidRequestDone() {
			Assert.IsNotNull(requestHandler.FromUrl);
			Assert.AreNotEqual("", requestHandler.FromUrl);
			Assert.IsTrue(requestHandler.ToFile.StartsWith(Constants.DEBUG_SYMBOL_PATH), "Invalid DebugSymbol target: " + requestHandler.ToFile);
			Assert.IsTrue(requestHandler.ToFile.EndsWith($"some-md5-hash{Path.DirectorySeparatorChar}somelib.dbg"), $"Invalid DebugSymbol target: {requestHandler.ToFile}");
		}
	}
}
