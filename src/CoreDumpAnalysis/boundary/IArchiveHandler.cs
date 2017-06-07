namespace SuperDump.Analyzer.Linux {
	public interface IArchiveHandler {
		bool TryExtract(string file);
	}
}