using System;

namespace SuperDumpService.Helpers {
	public class NoDumpInZipException : Exception {
		public NoDumpInZipException() { }

		public NoDumpInZipException(string message) : base(message) { }

		public NoDumpInZipException(string message, Exception inner) : base(message, inner) { }
	}
}
