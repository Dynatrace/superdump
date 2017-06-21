using System.IO;
using Thinktecture.IO;

namespace SuperDump.Analyzer.Linux.Boundary {
	public interface IArchiveHandler {
		bool TryExtractAndDelete(IFileInfo file);
	}
}