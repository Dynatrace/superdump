using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Thinktecture.IO;

namespace SuperDump.Analyzer.Linux.Boundary {
	public interface IFilesystem {
		IFileInfo GetFile(string path);
		string Md5FromFile(string path);
		Task HttpContentToFile(HttpContent stream, string targetFile);
		void WriteToFile(string filepath, string content);
		IEnumerable<string> ReadLines(IFileInfo file);
		IDirectoryInfo GetDirectory(string inputFile);
		void Move(string source, string target);
		void Delete(string path);
	}
}