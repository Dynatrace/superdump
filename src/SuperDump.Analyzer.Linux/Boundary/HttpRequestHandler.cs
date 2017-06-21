using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SuperDump.Analyzer.Linux.Boundary {
	public class HttpRequestHandler : IHttpRequestHandler {

		private readonly IFilesystem filesystem;

		public HttpRequestHandler(IFilesystem filesystem) {
			this.filesystem = filesystem ?? throw new ArgumentNullException("Filesystem must not be null!");
		}

		public async Task<bool> DownloadFromUrlAsync(string url, string targetFile) {
			HttpClient httpClient = new HttpClient();
			HttpResponseMessage response = await httpClient.GetAsync(url);
			if (response.IsSuccessStatusCode) {
				filesystem.HttpContentToFile(response.Content, targetFile);
				return true;
			}
			return false;
		}
	}
}
