using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SuperDump.Analyzer.Linux.Analysis;
using SuperDump.Analyzer.Linux.Boundary;
using SuperDump.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Thinktecture.IO;

namespace SuperDump.Analyzer.Linux.Test.Analysis {
	[TestClass]
	public class CoreLogAnalyzerTest {
		private const string COREDUMP_PATH = "/some-path/dumps/dump.core";
		private const string COREDUMP_LOG = "/some-path/dumps/dump.log";
		private const string DIR_FULL_NAME = "/some-path/dumps/";
		private const string MODULE_FILENAME = "my-module.so";

		private const string DEFAULT_MODULE_VERSION = "12.34";
		private const string UPDATED_MODULE_VERSION = "98.76";

		private CoreLogAnalyzer analyzer;

		private Mock<IFilesystem> filesystem;
		private IList<SDModule> modules;
		private Mock<IFileInfo> coredump;
		private Mock<IFileInfo> corelog;
		private SDCDModule module;

		[TestInitialize]
		public void Init() {
			module = new SDCDModule() {
				Version = DEFAULT_MODULE_VERSION,
				FileName = MODULE_FILENAME,
				FilePath = MODULE_FILENAME
			};
			modules = new List<SDModule> {
				module
			};

			coredump = new Mock<IFileInfo>();
			var dir = new Mock<IDirectoryInfo>();
			coredump.Setup(cd => cd.Directory).Returns(dir.Object);
			dir.Setup(d => d.FullName).Returns(DIR_FULL_NAME);
			coredump.Setup(cd => cd.FullName).Returns(COREDUMP_PATH);

			filesystem = new Mock<IFilesystem>(MockBehavior.Strict);
			corelog = new Mock<IFileInfo>();
			filesystem.Setup(fs => fs.GetFile(COREDUMP_LOG)).Returns(corelog.Object);

			this.analyzer = new CoreLogAnalyzer(filesystem.Object, coredump.Object, modules);
		}

		[TestMethod]
		public void TestWithoutCoreLog() {
			corelog.Setup(cl => cl.Exists).Returns(false);
			analyzer.Analyze();
			filesystem.Verify(fs => fs.ReadLines(It.IsAny<IFileInfo>()), Times.Never());
			Assert.AreEqual(DEFAULT_MODULE_VERSION, module.Version);
		}

		[TestMethod]
		public void TestWithEmptyCoreLog() {
			corelog.Setup(cl => cl.Exists).Returns(true);
			filesystem.Setup(fs => fs.ReadLines(corelog.Object)).Returns(new List<string>());
			analyzer.Analyze();
			filesystem.Verify(fs => fs.ReadLines(corelog.Object), Times.Once());
			Assert.AreEqual(DEFAULT_MODULE_VERSION, module.Version);
		}

		[TestMethod]
		public void TestVersionUpdate() {
			var lines = new List<string>() {
				"dummy line",
				$"  some   strings-/% /lib/x86_64-linux-gnu/{MODULE_FILENAME} ({UPDATED_MODULE_VERSION})",
				"dummy line"
			};
			corelog.Setup(cl => cl.Exists).Returns(true);
			filesystem.Setup(fs => fs.ReadLines(corelog.Object)).Returns(lines);
			analyzer.Analyze();
			filesystem.Verify(fs => fs.ReadLines(corelog.Object), Times.Once());
			Assert.AreEqual(UPDATED_MODULE_VERSION, module.Version);
		}
	}
}
