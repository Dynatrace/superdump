using System;
using System.Threading.Tasks;
using SuperDumpService.Models;
using SuperDumpService.Helpers;
using System.IO;
using System.Net.Http;

namespace SuperDumpService.Services {
	public class DownloadService {
		private readonly PathHelper pathHelper;

		public DownloadService(PathHelper pathHelper) {
			this.pathHelper = pathHelper;
		}

		public async Task<TempDirectoryHandle> Download(string bundleId, string url, string filename) {
			if (Utility.IsLocalFile(url) && IsAlreadyInUploadsDir(url)) {
				return new TempDirectoryHandle(new DirectoryInfo(Path.GetDirectoryName(url)));
			} else {
				DirectoryInfo dir = FindUniqueSubDirectoryName(new DirectoryInfo(pathHelper.GetUploadsDir()));
				dir.Create();
				var file = new FileInfo(Path.Combine(dir.FullName, Path.GetFileName(filename)));

				try {
					if (Utility.IsLocalFile(url)) {
						await Utility.CopyFile(new FileInfo(url), file);
					} else {
						// download
						using (var client = new HttpClient()) {
							using (var download = await client.GetAsync(url)) {
								using (var stream = await download.Content.ReadAsStreamAsync()) {
									using (var outfile = File.OpenWrite(file.FullName)) {
										await stream.CopyToAsync(outfile);
									}
								}
							}
						}
					}
				} catch(Exception e) {
					Console.WriteLine($"Failed to download file from {url}. Deleting the download directory ...");
					dir.Delete(true);
					throw e;
				}
				return new TempDirectoryHandle(dir);
			}
		}

		private bool IsAlreadyInUploadsDir(string url) {
			return Utility.IsSubdirectoryOf(new DirectoryInfo(pathHelper.GetUploadsDir()), new DirectoryInfo(Path.GetDirectoryName(url)));
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

		private static DirectoryInfo FindUniqueSubDirectoryName(DirectoryInfo dir) {
			DirectoryInfo subdir;
			do {
				string rand = RandomIdGenerator.GetRandomId(1, 10);
				subdir = new DirectoryInfo(Path.Combine(dir.FullName, rand));
			} while (subdir.Exists);
			return subdir;
		}
	}
}
