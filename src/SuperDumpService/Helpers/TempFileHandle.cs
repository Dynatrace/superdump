using System;
using System.IO;

namespace SuperDumpService.Helpers {
	public class TempFileHandle : IDisposable {
		private readonly TempDirectoryHandle parentDirectory;
		public FileInfo File { get; internal set; }

		/// <summary>
		/// Deletes the file given, when disposed. Except "delete=false".
		/// </summary>
		public TempFileHandle(FileInfo file, TempDirectoryHandle parentDirectory) {
			this.File = file;
			this.parentDirectory = parentDirectory;
		}

		public void Dispose() {
			File.Delete();
			this.parentDirectory.Dispose();
		}
	}
}