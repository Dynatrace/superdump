using System;

namespace SuperDump.Common {
	public class ProcessRunnerException : Exception {
		public ProcessRunnerException() {
		}

		public ProcessRunnerException(string message) : base(message) {
		}

		public ProcessRunnerException(string message, Exception innerException) : base(message, innerException) {
		}
	}
}