using System.Collections.Generic;

namespace CoreDumpAnalysis {
	public interface IFilesystemHelper {
		void CreateSymbolicLink(string targetDebugFile, string debugSymbolPath);
		List<string> FilesInDirectory(string directory);
		string GetParentDirectory(string dir);
		bool FileExists(string path);
	}
}