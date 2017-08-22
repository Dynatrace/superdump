using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SuperDump.Analyzer.Linux.Boundary {
	public class HttpRequestHandler : IHttpRequestHandler {

		private readonly IFilesystem filesystem;

		public HttpRequestHandler(IFilesystem filesystem) {
			this.filesystem = filesystem ?? throw new ArgumentNullException("Filesystem must not be null!");
		}

		public async Task<bool> DownloadFromUrlAsync(string url, string targetFile) {
			return await DownloadFromUrlAsync(url, targetFile, null);
		}

		public async Task<bool> DownloadFromUrlAsync(string url, string targetFile, string username, string password) {
			string encodedAuthentication = Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Format("{0}:{1}", username, password)));
			return await DownloadFromUrlAsync(url, targetFile, encodedAuthentication);
		}

		public async Task<bool> DownloadFromUrlAsync(string url, string targetFile, string authentication) {
			HttpClient httpClient = new HttpClient();
			if (authentication != null) {
				httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authentication);
			}
			HttpResponseMessage response = await httpClient.GetAsync(url);
			if (response.IsSuccessStatusCode) {
				string targetDir = targetFile.Substring(0, targetFile.LastIndexOf('/'));
				Directory.CreateDirectory(targetDir);
				await filesystem.HttpContentToFile(response.Content, targetFile);
				return true;
			}
			return false;
		}
	}
}
