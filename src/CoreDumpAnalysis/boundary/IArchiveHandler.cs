namespace SuperDump.Analyzer.Linux.Boundary {
	public interface IArchiveHandler {
		bool TryExtract(string file);
	}
}