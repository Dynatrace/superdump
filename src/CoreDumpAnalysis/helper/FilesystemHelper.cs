using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CoreDumpAnalysis {
	public class FilesystemHelper : IFilesystemHelper {
		public String GetParentDirectory(String dir) {
			int idx = dir.LastIndexOfAny(new char[] { '/', '\\' });
			if (idx >= 0) {
				return dir.Substring(0, idx) + "/";
			}
			return "./";
		}

		public List<String> FilesInDirectory(String directory) {
			List<String> files = new List<string>();
			FilesInDirectoryRec(directory, files);
			return files;
		}

		private void FilesInDirectoryRec(String directory, List<String> current) {
			foreach (string f in Directory.GetFiles(directory)) {
				current.Add(f);
			}
			try {
				foreach (string d in Directory.GetDirectories(directory)) {
					current.AddRange(FilesInDirectory(d));
				}
			} catch (System.Exception excpt) {
				Console.WriteLine(excpt.Message);
			}
		}

		public void CreateSymbolicLink(string targetDebugFile, string debugSymbolPath) {
			var process = new Process {
				StartInfo = new ProcessStartInfo {
					FileName = "ln",
					Arguments = "-s " + targetDebugFile + " " + debugSymbolPath,
					UseShellExecute = false,
					CreateNoWindow = true
				}
			};
			process.Start();
			process.WaitForExit();
		}

		public bool FileExists(string path) {
			return File.Exists(path);
		}
	}
}
