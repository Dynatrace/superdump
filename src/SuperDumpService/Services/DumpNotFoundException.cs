using System;

namespace SuperDumpService.Services {
	internal class DumpNotFoundException : Exception {
		public DumpNotFoundException() {
		}

		public DumpNotFoundException(string message) : base(message) {
		}

		public DumpNotFoundException(string message, Exception innerException) : base(message, innerException) {
		}
	}
}