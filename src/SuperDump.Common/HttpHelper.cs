using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SuperDump.Common {
	public static class HttpHelper {
		public static async Task<bool> Download(string url, string outputFile) {
			Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
			using (var client = new HttpClient()) {
				using (var download = await client.GetAsync(url)) {
					if (!download.IsSuccessStatusCode) return false;
					using (var stream = await download.Content.ReadAsStreamAsync()) {
						using (var outfile = File.OpenWrite(outputFile)) {
							await stream.CopyToAsync(outfile);
							return true;
						}
					}
				}
			}
		}
	}
}
