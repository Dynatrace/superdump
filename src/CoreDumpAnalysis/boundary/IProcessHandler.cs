using SuperDump.Analyzer.Linux.boundary;
using System.IO;

namespace SuperDump.Analyzer.Linux {
	public interface IProcessHandler {
		StreamReader StartProcessAndRead(string fileName, string arguments);
		ProcessStreams StartProcessAndReadWrite(string fileName, string arguments);
	}
}