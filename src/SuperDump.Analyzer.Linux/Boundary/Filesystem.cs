using SuperDump.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Thinktecture.IO;
using Thinktecture.IO.Adapters;

namespace SuperDump.Analyzer.Linux.Boundary {
	public class Filesystem : IFilesystem {
		public string Md5FromFile(string path) {
			using (var md5 = MD5.Create()) {
				using (var stream = File.OpenRead(path)) {
					return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
				}
			}
		}

		public async Task HttpContentToFile(HttpContent inputstream, string filepath) {
			Directory.CreateDirectory(Path.GetDirectoryName(filepath));
			using (FileStream stream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.None)) {
				await inputstream.CopyToAsync(stream);
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

		public IDirectoryInfo GetDirectory(string path) {
			return new DirectoryInfoAdapter(path);
		}

		public void Move(string source, string target) {
			File.Move(source, target);
		}

		public void Delete(string path) {
			File.Delete(path);
		}
	}
}
