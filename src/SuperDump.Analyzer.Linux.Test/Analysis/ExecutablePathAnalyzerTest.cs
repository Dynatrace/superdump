using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SuperDump.Analyzer.Linux.Analysis;
using SuperDump.Analyzer.Linux.Boundary;
using SuperDump.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Thinktecture.IO;

namespace SuperDump.Analyzer.Linux.Test.Analysis {
	[TestClass]
	public class ExecutablePathAnalyzerTest {
		private const string DEFAULT_FILENAME = "some-other-exe";
		private const string EXECUTABLE = "my-exec";

		private ExecutablePathAnalyzer analyzer;

		private Mock<IFilesystem> filesystem;
		private SDCDSystemContext context;

		private Mock<IFileInfo> summary;
		private Mock<IFileInfo> execFile;
		private Mock<IFileInfo> nonExistentFile;

		[TestInitialize]
		public void Init() {
			filesystem = new Mock<IFilesystem>(MockBehavior.Strict);
			context = new SDCDSystemContext();
			context.FileName = DEFAULT_FILENAME;
			analyzer = new ExecutablePathAnalyzer(filesystem.Object, context);

			summary = new Mock<IFileInfo>();
			filesystem.Setup(fs => fs.GetFile(Constants.SUMMARY_TXT)).Returns(summary.Object);

			execFile = new Mock<IFileInfo>();
			execFile.Setup(f => f.Exists).Returns(true);

			nonExistentFile = new Mock<IFileInfo>();
			nonExistentFile.Setup(f => f.Exists).Returns(false);
		}

		[TestMethod]
		public void TestWithSummaryFile() {			
			summary.Setup(s => s.Exists).Returns(true);
			filesystem.Setup(fs => fs.ReadLines(summary.Object)).Returns(new List<string>() {
				"dummy-line", $"executablePath: {EXECUTABLE}", "dummy-line"
			});
			var execFile = new Mock<IFileInfo>();
			execFile.Setup(f => f.Exists).Returns(true);
			filesystem.Setup(fs => fs.GetFile($".{EXECUTABLE}")).Returns(execFile.Object);

			analyzer.Analyze();
			Assert.AreEqual($".{EXECUTABLE}", context.FileName);
		}

		[TestMethod]
		public void TestFileNameFromContext() {
			summary.Setup(f => f.Exists).Returns(false);
			filesystem.Setup(fs => fs.GetFile(EXECUTABLE)).Returns(execFile.Object);
			filesystem.Setup(fs => fs.GetFile("." + EXECUTABLE)).Returns(nonExistentFile.Object);
			context.FileName = EXECUTABLE;

			analyzer.Analyze();
			Assert.AreEqual(EXECUTABLE, context.FileName);
		}

		[TestMethod]
		public void TestFileNameFromArgs() {
			summary.Setup(f => f.Exists).Returns(false);
			filesystem.Setup(fs => fs.GetFile(DEFAULT_FILENAME)).Returns(nonExistentFile.Object);
			filesystem.Setup(fs => fs.GetFile($".{DEFAULT_FILENAME}")).Returns(nonExistentFile.Object);
			filesystem.Setup(fs => fs.GetFile($".{EXECUTABLE}")).Returns(nonExistentFile.Object);
			filesystem.Setup(fs => fs.GetFile(EXECUTABLE)).Returns(execFile.Object);

			context.Args = $"{EXECUTABLE} -p 8080";

			analyzer.Analyze();
			Assert.AreEqual(EXECUTABLE, context.FileName);
		}
	}
}
