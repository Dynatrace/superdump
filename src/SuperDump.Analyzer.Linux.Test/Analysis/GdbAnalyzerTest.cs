using SuperDump.Analyzer.Linux;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SuperDump.Models;
using SuperDumpModels;
using System;
using System.Collections;
using System.Collections.Generic;
using SuperDump.Analyzer.Linux.Analysis;
using Moq;
using Thinktecture.IO;
using SuperDump.Analyzer.Linux.Boundary;
using System.IO;

namespace SuperDump.Analyzer.Linux.Test {
	[TestClass]
	public class GdbAnalyzerTest {

		private static string PATH = "path/";

		private GdbAnalyzer analysis;

		private Mock<IFilesystem> filesystem;
		private ProcessHandlerDouble processHandler;
		private SDResult analysisResult;

		[TestInitialize]
		public void Init() {
			filesystem = new Mock<IFilesystem>();
			processHandler = new ProcessHandlerDouble();
			analysisResult = new SDResult();
			var coredump = new Mock<IFileInfo>();
			coredump.Setup(c => c.FullName).Returns(PATH + "dump.core");
			analysis = new GdbAnalyzer(filesystem.Object, processHandler, coredump.Object, analysisResult);

			this.analysisResult.ThreadInformation = new Dictionary<uint, SDThread>();
			SDThread thread = new SDThread();
			var frames = new List<SDCombinedStackFrame> {
				new SDCDCombinedStackFrame("module", "method", 0, 0, 0, 0, 0, null)
			};
			thread.StackTrace = new SDCombinedStackTrace(frames);
			this.analysisResult.ThreadInformation.Add(0, thread);
			SDCDSystemContext context = new SDCDSystemContext() {
				FileName = PATH + "my-executable"
			};
			analysisResult.SystemContext = context;
		}

		private void RunAnalysisAndVerify(string cmd, string err) {
			processHandler.SetOutputForCommand("gdb", cmd);
			processHandler.SetErrorForCommand("gdb", err);
			analysis.Analyze();

			VerifyWrittenFiles(cmd, err == "" ? null : err);
		}

		[TestMethod]
		public void TestSimpleThread() {
			string cmd = "random init messages..." + Environment.NewLine +
				">>thread 0" + Environment.NewLine +
				 ">>select 0" + Environment.NewLine +
				 ">>info args" + Environment.NewLine +
				 ">>info locals" + Environment.NewLine +
				 ">>finish frame" + Environment.NewLine +
				 ">>finish thread" + Environment.NewLine;
			RunAnalysisAndVerify(cmd, "something bad happened");
			VerifySingleFrameHasVars(new Dictionary<string, string>(), new Dictionary<string, string>());
		}

		[TestMethod]
		public void TestThreadWithArgs() {
			string cmd = "random init messages..." + Environment.NewLine +
				">>thread 0" + Environment.NewLine +
				 ">>select 0" + Environment.NewLine +
				 ">>info args" + Environment.NewLine +
				 "(gdb) my_var = 1234" + Environment.NewLine +
				 "(gdb) other_var = \"hello world\"" + Environment.NewLine +
				 ">>info locals" + Environment.NewLine +
				 ">>finish frame" + Environment.NewLine +
				 ">>finish thread" + Environment.NewLine;
			RunAnalysisAndVerify(cmd, "");

			Dictionary<string, string> expectedArgs = new Dictionary<string, string> {
				{ "my_var", "1234" }, { "other_var", "\"hello world\"" }
			};
			VerifySingleFrameHasVars(expectedArgs, new Dictionary<string, string>());
		}

		[TestMethod]
		public void TestThreadWithLocals() {
			string cmd = "init messages..." + Environment.NewLine +
				">>thread 0" + Environment.NewLine +
				 ">>select 0" + Environment.NewLine +
				 ">>info args" + Environment.NewLine +
				 ">>info locals" + Environment.NewLine +
				 "(gdb) my_var = 1234" + Environment.NewLine +
				 "(gdb) other_var = \"hello world\"" + Environment.NewLine +
				 ">>finish frame" + Environment.NewLine +
				 ">>finish thread" + Environment.NewLine;
			RunAnalysisAndVerify(cmd, "");

			Dictionary<string, string> expectedLocals = new Dictionary<string, string> {
				{ "my_var", "1234" }, { "other_var", "\"hello world\"" }
			};
			VerifySingleFrameHasVars(new Dictionary<string, string>(), expectedLocals);
		}

		private void VerifyWrittenFiles(string cmd, string err) {
			filesystem.Verify(fs => fs.WriteToFile(GdbAnalyzer.GDB_OUT_FILE, cmd));
			if (err != null) {
				filesystem.Verify(fs => fs.WriteToFile(GdbAnalyzer.GDB_ERR_FILE, err));
			} else {
				filesystem.Verify(fs => fs.WriteToFile(GdbAnalyzer.GDB_ERR_FILE, It.IsAny<string>()), Times.Never());
			}
		}

		private void VerifySingleFrameHasVars(Dictionary<string, string> expectedArgs, Dictionary<string, string> expectedLocals) {
			SDCDCombinedStackFrame frame = (SDCDCombinedStackFrame)analysisResult.ThreadInformation[0].StackTrace[0];
			CollectionAssert.AreEquivalent(expectedArgs, (ICollection)frame.Args);
			CollectionAssert.AreEquivalent(expectedLocals, (ICollection)frame.Locals);
		}
	}
}
