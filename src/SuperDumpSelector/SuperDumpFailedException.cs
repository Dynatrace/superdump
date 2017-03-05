using System;
using System.Runtime.Serialization;

namespace SuperDumpSelector {
	[Serializable]
	internal class SuperDumpFailedException : Exception {
		public SuperDumpFailedException() {
		}

		public SuperDumpFailedException(string message) : base(message) {
		}

		public SuperDumpFailedException(string message, Exception innerException) : base(message, innerException) {
		}

		protected SuperDumpFailedException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}