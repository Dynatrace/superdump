using System;
using System.IO;

namespace SuperDumpService.Models {
	public class TempFileHandle : IDisposable {
		private bool delete;

		public string Path { get; internal set; }

		/// <summary>
		/// Deletes the file given, when disposed. Except "delete=false".
		/// </summary>
		public TempFileHandle(string path, bool delete = true) {
			this.Path = path;
			this.delete = delete;
		}

		public void Dispose() {
			File.Delete(Path);
		}
	}
}