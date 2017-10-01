using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SuperDump.Analyzer.Linux.Boundary {
	public class ProcessStreams : IDisposable {
		private StreamReader _output;
		public StreamReader Output {
			get {
				if (disposed) {
					throw new InvalidOperationException("Cannot access a stream that was already disposed!");
				}
				return _output;
			}
			private set { _output = value; }
		}
		private StreamWriter _input;
		public StreamWriter Input {
			get {
				if (disposed) {
					throw new InvalidOperationException("Cannot access a stream that was already disposed!");
				}
				return _input;
			}
			private set { _input = value; }
		}
		private StreamReader _error;
		public StreamReader Error {
			get {
				if (disposed) {
					throw new InvalidOperationException("Cannot access a stream that was already disposed!");
				}
				return _error;
			}
			private set { _error = value; }
		}

		private readonly Action disposer;

		private bool disposed;

		public ProcessStreams(StreamReader output, StreamWriter input, StreamReader error, Action disposer) {
			this.Output = output;
			this.Input = input;
			this.Error = error;
			this.disposer = disposer;
			this.disposed = false;
		}

		public void Dispose() {
			Output.Dispose();
			if(!(Input.BaseStream is MemoryStream)) {
				// MemoryStreams must not be closed! (required for tests only)
				Input.Dispose();
			}
			Error.Dispose();
			disposer?.Invoke();
			disposed = true;
		}
	}
}
