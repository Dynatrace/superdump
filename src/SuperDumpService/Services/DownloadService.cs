using System;
using System.Threading.Tasks;
using SuperDumpService.Models;
using SuperDumpService.Helpers;
using System.IO;
using System.Net.Http;

namespace SuperDumpService.Services {
	public class DownloadService {
		public async Task<TempFileHandle> Download(string bundleId, string url, string filename) {
			var uri = new Uri(url);
			
			//check if local or not
			if (!Utility.IsLocalFile(url)) {
				// download
				string tempPath = Path.Combine(FindUniqueFilename(PathHelper.GetBundleDownloadPath(filename)));
				using (var client = new HttpClient()) {
					using (var download = await client.GetAsync(uri)) {
						using (var stream = await download.Content.ReadAsStreamAsync()) {
							using (var outfile = File.OpenWrite(tempPath)) {
								await stream.CopyToAsync(outfile);
							}
						}
					}
				}
				return new TempFileHandle(new FileInfo(tempPath));
			} else {
				return new TempFileHandle(new FileInfo(url), false); // in case it's a local file, don't delete it!
			}
		}

		private static string FindUniqueFilename(string fullpath) {
			int i = 1;
			string directory = Path.GetDirectoryName(fullpath);
			string filenameWoExtension = Path.GetFileNameWithoutExtension(fullpath);
			string filename = Path.GetFileName(fullpath);
			string extension = Path.GetExtension(filename);

			while (File.Exists(fullpath)) {
				fullpath = Path.Combine(directory, $"{filenameWoExtension}_{i}{extension}");
				i++;
			}
			return fullpath;
		}
	}
}
