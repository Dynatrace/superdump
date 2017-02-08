using System;

namespace SuperDumpService.Services {
	internal class DirectoryAlreadyExistsException : Exception {
		public DirectoryAlreadyExistsException() {
		}

		public DirectoryAlreadyExistsException(string message) : base(message) {
		}

		public DirectoryAlreadyExistsException(string message, Exception innerException) : base(message, innerException) {
		}
	}
}