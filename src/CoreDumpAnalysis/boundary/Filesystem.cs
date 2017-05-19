using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;

namespace CoreDumpAnalysis {
	public class Filesystem : IFilesystem {
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

		public string Md5FromFile(string path) {
			using (var md5 = MD5.Create()) {
				using (var stream = File.OpenRead(path)) {
					return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
				}
			}
		}

		public void HttpContentToFile(HttpContent inputstream, string filepath) {
			Directory.CreateDirectory(Path.GetDirectoryName(filepath));
			using (FileStream stream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.None)) {
				inputstream.CopyToAsync(stream);
			}
		}

		public long FileSize(string path) {
			return new FileInfo(path).Length;
		}
	}
}
