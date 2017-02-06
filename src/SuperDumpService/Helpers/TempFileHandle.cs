using System;
using System.IO;

namespace SuperDumpService.Helpers {
	public class TempFileHandle : IDisposable {
		private bool delete;

		public FileInfo File { get; internal set; }

		/// <summary>
		/// Deletes the file given, when disposed. Except "delete=false".
		/// </summary>
		public TempFileHandle(FileInfo file, bool delete = true) {
			this.File = file;
			this.delete = delete;
		}

		public void Dispose() {
			File.Delete();
		}
	}
}