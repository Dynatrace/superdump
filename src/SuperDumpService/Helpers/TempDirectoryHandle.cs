using System;
using System.IO;

namespace SuperDumpService.Helpers {
	public class TempDirectoryHandle : IDisposable {
		private bool delete;

		public DirectoryInfo Dir { get; internal set; }

		/// <summary>
		/// Deletes the directory given, when disposed. Except "delete=false".
		/// </summary>
		public TempDirectoryHandle(DirectoryInfo dir, bool delete = true) {
			this.Dir = dir;
			this.delete = delete;
		}

		public void Dispose() {
			Dir.Delete(true);
		}
	}
}