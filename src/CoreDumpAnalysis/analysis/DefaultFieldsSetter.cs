using SuperDump.Models;
using System;
using System.Collections.Generic;

namespace SuperDump.Analyzer.Linux
{
	public class DefaultFieldsSetter {
		private readonly SDResult analysisResult;

		public DefaultFieldsSetter(SDResult result) {
			this.analysisResult = result ?? throw new ArgumentNullException("SD Result must not be null!");
		}

		public void SetResultFields() {
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
