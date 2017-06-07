namespace SuperDump.Analyzer.Linux.Boundary {
	public interface IHttpRequestHandler {
		bool DownloadFromUrl(string url, string targetFile);
	}
}