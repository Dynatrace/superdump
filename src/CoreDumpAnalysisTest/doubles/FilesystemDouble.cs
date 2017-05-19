using CoreDumpAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;

namespace CoreDumpAnalysisTest {
	internal class FilesystemDouble : IFilesystem {

		private IList<string> existingFiles = new List<string>();
		private IDictionary<string, long> fileSizes = new Dictionary<string, long>();

		public bool LinkCreated { get; private set; } = false;

		public string Md5 { get; set; }

		public void CreateSymbolicLink(string targetDebugFile, string debugSymbolPath) {
			LinkCreated = true;
		}

		public bool FileExists(string path) {
			return existingFiles.Contains(path);
		}

		public List<string> FilesInDirectory(string directory) {
			throw new NotImplementedException();
		}

		public long FileSize(string path) {
			return fileSizes[path];
		}

		public string GetParentDirectory(string dir) {
			throw new NotImplementedException();
		}

		public void HttpContentToFile(HttpContent stream, string targetFile) {
			throw new NotImplementedException();
		}

		public string Md5FromFile(string path) {
			return Md5;
		}

		public void SetFileSize(string path, long filesize) {
			fileSizes.Add(path, filesize);
		}

		public void SetFileExists(string path) {
			existingFiles.Add(path);
		}
	}
}
