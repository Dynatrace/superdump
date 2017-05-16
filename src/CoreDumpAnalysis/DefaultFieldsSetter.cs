using SuperDump.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoreDumpAnalysis
{
	class DefaultFieldsSetter {
		private readonly SDResult analysisResult;
		private readonly String coredump;

		public DefaultFieldsSetter(String coredump, SDResult result) {
			this.analysisResult = result ?? throw new ArgumentNullException("SD Result must not be null!");
			this.coredump = coredump ?? throw new ArgumentNullException("Coredump Path must not be null!");
		}

		public void DebugAndSetResultFields() {
			if (this.analysisResult.LastEvent == null) {
				this.analysisResult.LastEvent = new SDLastEvent("Unknown", "", 1);
			}
			if (this.analysisResult.ExceptionRecord == null) {
				this.analysisResult.ExceptionRecord = new List<SDClrException>();
			}
			if (this.analysisResult.DeadlockInformation == null) {
				this.analysisResult.DeadlockInformation = new List<SDDeadlockContext>();
			}
			if(this.analysisResult.MemoryInformation == null) {
				this.analysisResult.MemoryInformation = new Dictionary<ulong, SDMemoryObject>();
			}
		}
	}
}
