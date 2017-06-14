using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using Thinktecture.IO;
using Thinktecture.IO.Adapters;

namespace SuperDump.Analyzer.Linux.Boundary {
	public class Filesystem : IFilesystem {
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

		public void WriteToFile(string filepath, string content) {
			File.WriteAllText(filepath, content);
		}

		public IEnumerable<string> ReadLines(IFileInfo file) {
			return File.ReadLines(file.FullName);
		}

		public IFileInfo GetFile(string path) {
			return new FileInfoAdapter(path);
		}
	}
}
