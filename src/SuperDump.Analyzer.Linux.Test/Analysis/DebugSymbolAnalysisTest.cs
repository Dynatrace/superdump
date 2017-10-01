using SuperDump.Analyzer.Linux;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SuperDump.Models;
using System;
using System.Collections.Generic;
using SuperDump.Analyzer.Linux.Analysis;
using Moq;
using SuperDump.Analyzer.Linux.Boundary;
using Thinktecture.IO;
using System.IO;

namespace SuperDump.Analyzer.Linux.Test {
	[TestClass]
	public class DebugSymbolAnalysisTest {
		private const string DEBUG_SYMBOLS_PATH = "debug/info";
		private const string DEBUG_FILE_PATH = "some-dump/lib.dbg";

		private const string DEFAULT_MODULE_NAME = "module name";
		private const string DEFAULT_METHOD_NAME = "method name";

		private DebugSymbolAnalysis analysis;
		private Mock<IFileInfo> targetDebugFile;

		private SDResult result;
		private Mock<IFilesystem> filesystem;
		private ProcessHandlerDouble processHandler;

		[TestInitialize]
		public void InitAnalysis() {
			result = new SDResult();
			filesystem = new Mock<IFilesystem>();
			processHandler = new ProcessHandlerDouble();
			analysis = new DebugSymbolAnalysis(filesystem.Object, processHandler, result);

			targetDebugFile = new Mock<IFileInfo>();
			targetDebugFile.Setup(f => f.FullName).Returns(DEBUG_FILE_PATH);
			filesystem.Setup(fs => fs.GetFile(It.IsAny<string>())).Returns(targetDebugFile.Object);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestNullThreads() {
			PrepareSampleModule(1234, "acutal module name");
			analysis.Analyze();
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestNullModules() {
			PrepareSampleThread(1234);
			analysis.Analyze();
		}

		[TestMethod]
		public void TestNoBinary() {
			PrepareSampleModule(1234, "actual module name");
			PrepareSampleThread(1234);
			analysis.Analyze();
			Assert.AreEqual(DEFAULT_MODULE_NAME, GetFirstStackFrame().ModuleName);
		}

		[TestMethod]
		public void TestModuleNameUpdated() {
			PrepareModuleWithBinary(1234, "actual module name");
			PrepareSampleThread(1234);
			processHandler.SetOutputForCommand("addr2line", "??\n??:0");
			analysis.Analyze();
			Assert.AreEqual("actual module name", GetFirstStackFrame().ModuleName);
			Assert.IsNull(GetFirstStackFrame().SourceInfo);
		}

		[TestMethod]
		public void TestModuleSourceInfoUpdated() {
			PrepareModuleWithBinary(1234, "actual module name");
			PrepareSampleThread(1234);
			processHandler.SetOutputForCommand("addr2line", $"meth-name{Environment.NewLine}src/file.cpp:777");
			analysis.Analyze();
			Assert.AreEqual("src/file.cpp", GetFirstStackFrame().SourceInfo.File);
			Assert.AreEqual(777, GetFirstStackFrame().SourceInfo.Line);
			Assert.AreEqual("meth-name", GetFirstStackFrame().MethodName);
		}

		[TestMethod]
		public void TestModuleWithDebugInfo() {
			PrepareModuleWithDebugInfo(1234, "actual module name");
			PrepareSampleThread(1234);
			processHandler.SetOutputForCommand("addr2line", $"meth-name{Environment.NewLine}src/file.cpp:777");
			analysis.Analyze();
			Assert.AreEqual("src/file.cpp", GetFirstStackFrame().SourceInfo.File);
			Assert.AreEqual(777, GetFirstStackFrame().SourceInfo.Line);
			Assert.AreEqual("meth-name", GetFirstStackFrame().MethodName);
		}

		private SDCDModule PrepareModuleWithDebugInfo(ulong instrPtr, string moduleName) {
			var module = PrepareSampleModule(instrPtr, moduleName);
			module.LocalPath = "some/path";
			module.DebugSymbolPath = DEBUG_SYMBOLS_PATH;
			return module;
		}

		private SDCDModule PrepareModuleWithBinary(ulong instrPtr, string moduleName) {
			var module = PrepareSampleModule(instrPtr, moduleName);
			module.LocalPath = "some/path";
			return module;
		}

		private SDCDModule PrepareSampleModule(ulong instrPtr, string moduleName) {
			result.SystemContext = new SDCDSystemContext();
			result.SystemContext.Modules = new List<SDModule>();

			SDCDModule module = new SDCDModule() {
				StartAddress = instrPtr - 1,
				EndAddress = instrPtr + 1,
				FileName = moduleName
			};
			result.SystemContext.Modules.Add(module);
			return module;
		}

		private void PrepareSampleThread(ulong instrPtr) {
			result.ThreadInformation = new Dictionary<uint, SDThread>();
			SDThread thread = new SDThread(1);
			IList<SDCombinedStackFrame> stackFrames = new List<SDCombinedStackFrame>();
			stackFrames.Add(new SDCombinedStackFrame(StackFrameType.Native, DEFAULT_MODULE_NAME, DEFAULT_METHOD_NAME, 42, instrPtr, 42, 42, null, 42, null));
			thread.StackTrace = new SDCombinedStackTrace(stackFrames);
			result.ThreadInformation.Add(1, thread);
		}

		private SDCombinedStackFrame GetFirstStackFrame() {
			var threadInfo = result.ThreadInformation;
			SDThread thread;
			threadInfo.TryGetValue(1, out thread);
			var traces = thread.StackTrace;
			var traceEnumerator = traces.GetEnumerator();
			traceEnumerator.MoveNext();
			return traceEnumerator.Current;
		}
	}
}
