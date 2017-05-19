namespace CoreDumpAnalysis {
	public interface IArchiveHandler {
		bool TryExtract(string file);
	}
}