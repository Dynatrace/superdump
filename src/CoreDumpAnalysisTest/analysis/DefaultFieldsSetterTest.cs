using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CoreDumpAnalysis;
using SuperDump.Models;

namespace CoreDumpAnalysisTest {
	[TestClass]
	public class DefaultFieldsSetterTest {

		private SDResult result;
		private DefaultFieldsSetter setter;
		
		[TestInitialize]
		public void InitSetter() {
			result = new SDResult();
			setter = new DefaultFieldsSetter(result);
		}

		[TestMethod]
		public void TestFieldInitialization() {
			setter.SetResultFields();
			Assert.AreEqual(0, result.ExceptionRecord.Count);
			Assert.AreEqual(0, result.DeadlockInformation.Count);
			Assert.AreEqual(0, result.MemoryInformation.Count);
			Assert.IsNull(result.LastEvent);
		}
	}
}
