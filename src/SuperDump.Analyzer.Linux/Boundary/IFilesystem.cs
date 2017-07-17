using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Thinktecture.IO;

namespace SuperDump.Analyzer.Linux.Boundary {
	public interface IFilesystem {
		IFileInfo GetFile(string path);
		void CreateSymbolicLink(string targetDebugFile, string debugSymbolPath);
		string Md5FromFile(string path);
		Task HttpContentToFile(HttpContent stream, string targetFile);
		void WriteToFile(string filepath, string content);
		IEnumerable<string> ReadLines(IFileInfo file);
		IDirectoryInfo GetDirectory(string inputFile);
	}
}