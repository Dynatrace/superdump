using System.IO;

namespace CoreDumpAnalysis {
	public interface IProcessHandler {
		StreamReader StartProcessAndRead(string fileName, string arguments);
	}
}