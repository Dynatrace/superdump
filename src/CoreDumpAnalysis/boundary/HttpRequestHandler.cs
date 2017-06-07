using System;
using System.Net.Http;

namespace SuperDump.Analyzer.Linux.boundary {
	public class HttpRequestHandler : IHttpRequestHandler {

		private readonly IFilesystem filesystem;

		public HttpRequestHandler(IFilesystem filesystem) {
			this.filesystem = filesystem ?? throw new ArgumentNullException("Filesystem must not be null!");
		}

		public bool DownloadFromUrl(string url, string targetFile) {
			HttpClient httpClient = new HttpClient();
			var download = httpClient.GetAsync(url).ContinueWith(
				request => {
					HttpResponseMessage response = request.Result;
					if (request.Result.IsSuccessStatusCode) {
						filesystem.HttpContentToFile(response.Content, targetFile);
						return true;
					}
					return false;
				});
			download.Wait();
			return download.Result;
		}
	}
}
