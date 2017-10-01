using System.Threading.Tasks;

namespace SuperDump.Analyzer.Linux.Boundary {
	public interface IHttpRequestHandler {
		Task<bool> DownloadFromUrlAsync(string url, string targetFile);
		Task<bool> DownloadFromUrlAsync(string url, string targetFile, string username, string password);
		Task<bool> DownloadFromUrlAsync(string url, string targetFile, string authentication);
	}
}