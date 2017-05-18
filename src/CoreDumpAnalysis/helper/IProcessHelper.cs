using System.IO;

namespace CoreDumpAnalysis {
	public interface IProcessHelper {
		StreamReader StartProcessAndRead(string fileName, string arguments);
	}
}