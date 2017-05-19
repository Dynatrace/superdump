namespace CoreDumpAnalysis.boundary {
	public interface IHttpRequestHandler {
		bool DownloadFromUrl(string url, string targetFile);
	}
}