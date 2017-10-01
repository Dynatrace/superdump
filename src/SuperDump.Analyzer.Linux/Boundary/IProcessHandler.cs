using System.IO;
using System.Threading.Tasks;
using Thinktecture.IO;

namespace SuperDump.Analyzer.Linux.Boundary {
	public interface IProcessHandler {
		Task<string> ExecuteProcessAndGetOutputAsync(string executable, string arguments);
		ProcessStreams StartProcessAndReadWrite(string executable, string arguments);
	}
}