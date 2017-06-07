namespace SuperDump.Analyzer.Linux.boundary {
	public interface IHttpRequestHandler {
		bool DownloadFromUrl(string url, string targetFile);
	}
}