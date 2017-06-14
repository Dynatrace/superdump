using SuperDump.Analyzer.Linux;
using SuperDump.Analyzer.Linux.Boundary;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace SuperDump.Analyzer.Linux.Test {
	internal class FilesystemDouble {

		public IList<string> ExistingFiles = new List<string>();
		public IDictionary<string, long> FileSizes = new Dictionary<string, long>();
		public IDictionary<string, string> FileContents = new Dictionary<string, string>();

		public bool LinkCreated { get; private set; } = false;

		public string Md5 { get; set; }

		public void CreateSymbolicLink(string targetDebugFile, string debugSymbolPath) {
			LinkCreated = true;
		}

		public bool FileExists(string path) {
			return ExistingFiles.Contains(path);
		}

		public List<string> FilesInDirectory(string directory) {
			throw new NotImplementedException();
		}

		public long FileSize(string path) {
			return FileSizes[path];
		}

		public string GetParentDirectory(string dir) {
			int lastSlash = dir.LastIndexOf('/');
			if (lastSlash > 0) {
				return dir.Substring(0, dir.LastIndexOf('/'));
			}
			return dir;
		}

		public void HttpContentToFile(HttpContent stream, string targetFile) {
			throw new NotImplementedException();
		}

		public string Md5FromFile(string path) {
			return Md5;
		}

		public IEnumerable<string> ReadLines(string file) {
			return FileContents[file].Split('\n');
		}

		public void WriteToFile(string filepath, string content) {
			FileContents[filepath] = content;
		}
	}
}
