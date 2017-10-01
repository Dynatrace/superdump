using System;
using System.IO;
using System.Threading;

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
			// If deleting the directory fails, let's keep trying for another second because there might be some temporary process
			// accessing the directory causing an IOException ('file is in use by another process ...')
			for (int i = 0; i < 10; i++) {
				try {
					Dir.Delete(true);
					return;
				} catch (IOException e) {
					Console.WriteLine($"[{i}] Failed to delete temporary directory due to {e.GetType().ToString()}: {e.Message}");
					Thread.Sleep(100);
				}
			}
			Console.WriteLine($"Failed to delete temporary directory {Dir.FullName}!");
		}
	}
}