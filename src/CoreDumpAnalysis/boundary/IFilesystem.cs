using System.Collections.Generic;
using System.Net.Http;

namespace CoreDumpAnalysis {
	public interface IFilesystem {
		void CreateSymbolicLink(string targetDebugFile, string debugSymbolPath);
		List<string> FilesInDirectory(string directory);
		string GetParentDirectory(string dir);
		bool FileExists(string path);
		long FileSize(string path);
		string Md5FromFile(string path);
		void HttpContentToFile(HttpContent stream, string targetFile);
		void WriteToFile(string filepath, string content);
		IEnumerable<string> ReadLines(string file);
	}
}