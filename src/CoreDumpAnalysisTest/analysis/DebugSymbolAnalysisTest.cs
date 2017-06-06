using CoreDumpAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SuperDump.Models;
using System;
using System.Collections.Generic;

namespace CoreDumpAnalysisTest {
	[TestClass]
	public class DebugSymbolAnalysisTest {
		private readonly string DEFAULT_MODULE_NAME = "module name";
		private readonly string DEFAULT_METHOD_NAME = "method name";

		private DebugSymbolAnalyzer analysis;

		private SDResult result;
		private FilesystemDouble filesystem;
		private ProcessHandlerDouble processHandler;

		[TestInitialize]
		public void InitAnalysis() {
			result = new SDResult();
			filesystem = new FilesystemDouble();
			processHandler = new ProcessHandlerDouble();
			analysis = new DebugSymbolAnalyzer(filesystem, processHandler, "dump.core", result);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestNullThreads() {
			PrepareSampleModule(1234, "acutal module name");
			analysis.DebugAndSetResultFields();
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestNullModules() {
			PrepareSampleThread(1234);
			analysis.DebugAndSetResultFields();
		}

		[TestMethod]
		public void TestNoBinary() {
			PrepareSampleModule(1234, "actual module name");
			PrepareSampleThread(1234);
			analysis.DebugAndSetResultFields();
			Assert.AreEqual(DEFAULT_MODULE_NAME, GetFirstStackFrame().ModuleName);
		}

		[TestMethod]
		public void TestModuleNameUpdated() {
			PrepareModuleWithBinary(1234, "actual module name");
			PrepareSampleThread(1234);
			processHandler.SetOutputForCommand("addr2line", "??\n??:0");
			analysis.DebugAndSetResultFields();
			Assert.AreEqual("actual module name", GetFirstStackFrame().ModuleName);
			Assert.IsNull(GetFirstStackFrame().SourceInfo);
		}

		[TestMethod]
		public void TestModuleSourceInfoUpdated() {
			PrepareModuleWithBinary(1234, "actual module name");
			PrepareSampleThread(1234);
			processHandler.SetOutputForCommand("addr2line", "meth-name\nsrc/file.cpp:777");
			analysis.DebugAndSetResultFields();
			Assert.AreEqual("src/file.cpp", GetFirstStackFrame().SourceInfo.File);
			Assert.AreEqual(777, GetFirstStackFrame().SourceInfo.Line);
			Assert.AreEqual("meth-name", GetFirstStackFrame().MethodName);
			Assert.IsFalse(filesystem.LinkCreated);
		}

		[TestMethod]
		public void TestModuleWithDebugInfo() {
			PrepareModuleWithDebugInfo(1234, "actual module name");
			PrepareSampleThread(1234);
			processHandler.SetOutputForCommand("addr2line", "meth-name\nsrc/file.cpp:777");
			analysis.DebugAndSetResultFields();
			Assert.AreEqual("src/file.cpp", GetFirstStackFrame().SourceInfo.File);
			Assert.AreEqual(777, GetFirstStackFrame().SourceInfo.Line);
			Assert.AreEqual("meth-name", GetFirstStackFrame().MethodName);
			Assert.IsTrue(filesystem.LinkCreated);
		}

		private SDCDModule PrepareModuleWithDebugInfo(ulong instrPtr, string moduleName) {
			var module = PrepareSampleModule(instrPtr, moduleName);
			module.LocalPath = "some/path";
			module.DebugSymbolPath = "debug/info";
			return module;
		}

		private SDCDModule PrepareModuleWithBinary(ulong instrPtr, string moduleName) {
			var module = PrepareSampleModule(instrPtr, moduleName);
			module.LocalPath = "some/path";
			return module;
		}

		private SDCDModule PrepareSampleModule(ulong instrPtr, string moduleName) {
			result.SystemContext = new SDSystemContext();
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
