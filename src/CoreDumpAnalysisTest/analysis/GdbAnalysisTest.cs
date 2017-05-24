using CoreDumpAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SuperDump.Models;
using SuperDumpModels;
using System;
using System.Collections;
using System.Collections.Generic;

namespace CoreDumpAnalysisTest {
	[TestClass]
	public class GdbAnalysisTest {

		private static string PATH = "path/";

		private GdbAnalysis analysis;

		private FilesystemDouble filesystem;
		private ProcessHandlerDouble processHandler;
		private SDResult analysisResult;

		[TestInitialize]
		public void Init() {
			filesystem = new FilesystemDouble();
			processHandler = new ProcessHandlerDouble();
			analysisResult = new SDResult();
			analysis = new GdbAnalysis(filesystem, processHandler, PATH + "dump.core", analysisResult);

			this.analysisResult.ThreadInformation = new Dictionary<uint, SDThread>();
			SDThread thread = new SDThread();
			var frames = new List<SDCombinedStackFrame> {
				new SDCDCombinedStackFrame("module", "method", 0, 0, 0, 0, 0, null)
			};
			thread.StackTrace = new SDCombinedStackTrace(frames);
			this.analysisResult.ThreadInformation.Add(0, thread);
		}

		private void RunAnalysisAndVerify(string cmd, string err) {
			processHandler.SetOutputForCommand("gdb", cmd);
			processHandler.SetErrorForCommand("gdb", err);
			analysis.DebugAndSetResultFields();

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
			Assert.AreEqual(cmd, filesystem.FileContents[GdbAnalysis.GDB_OUT_FILE]);
			if(err != null) {
				Assert.AreEqual(err, filesystem.FileContents[GdbAnalysis.GDB_ERR_FILE]);
			} else {
				Assert.IsFalse(filesystem.FileContents.ContainsKey(GdbAnalysis.GDB_ERR_FILE));
			}
		}

		private void VerifySingleFrameHasVars(Dictionary<string, string> expectedArgs, Dictionary<string, string> expectedLocals) {
			SDCDCombinedStackFrame frame = (SDCDCombinedStackFrame)analysisResult.ThreadInformation[0].StackTrace[0];
			CollectionAssert.AreEquivalent(expectedArgs, (ICollection)frame.Args);
			CollectionAssert.AreEquivalent(expectedLocals, (ICollection)frame.Locals);
		}
	}
}
