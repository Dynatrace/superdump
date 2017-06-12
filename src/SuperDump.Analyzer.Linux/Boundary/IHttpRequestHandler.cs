using System.Threading.Tasks;

namespace SuperDump.Analyzer.Linux.Boundary {
	public interface IHttpRequestHandler {
		Task<bool> DownloadFromUrlAsync(string url, string targetFile);
	}
}