using CoreDumpAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoreDumpAnalysisTest {
	internal class FilesystemHelperDouble : IFilesystemHelper {
		public bool LinkCreated { get; private set; } = false;

		public void CreateSymbolicLink(string targetDebugFile, string debugSymbolPath) {
			LinkCreated = true;
		}

		public bool FileExists(string path) {
			return false;
		}

		public List<string> FilesInDirectory(string directory) {
			throw new NotImplementedException();
		}

		public string GetParentDirectory(string dir) {
			throw new NotImplementedException();
		}
	}
}
