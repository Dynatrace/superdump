using System.IO;

namespace SuperDump.Analyzer.Linux.Boundary {
	public interface IProcessHandler {
		StreamReader StartProcessAndRead(string fileName, string arguments);
		ProcessStreams StartProcessAndReadWrite(string fileName, string arguments);
	}
}