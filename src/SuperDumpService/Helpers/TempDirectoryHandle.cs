using System;
using System.IO;

namespace SuperDumpService.Helpers {
	public class TempDirectoryHandle : IDisposable {

		public DirectoryInfo Dir { get; internal set; }

		/// <summary>
		/// Deletes the directory given, when disposed.
		/// </summary>
		public TempDirectoryHandle(DirectoryInfo dir) {
			this.Dir = dir;
		}

		public void Dispose() {
			Dir.Delete(true);
		}
	}
}