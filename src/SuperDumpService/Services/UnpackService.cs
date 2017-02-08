using SuperDumpService.Helpers;
using SuperDumpService.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.Services {
	public class UnpackService {
		private readonly object sync = new object();
		private readonly DirectoryInfo tempDir = new DirectoryInfo(PathHelper.GetUploadsDir());

		public UnpackService() {
			tempDir.Create();
		}

		public TempDirectoryHandle UnZip(FileInfo file, Func<string, bool> filter) {
			var outputDir = FindUniqueTempDir(Path.GetFileNameWithoutExtension(file.Name));
			var tempHandle = new TempDirectoryHandle(outputDir);
			try {
				using (ZipArchive zip = ZipFile.OpenRead(file.FullName)) {
					foreach (var entry in zip.Entries) {
						if (filter(entry.Name)) {
							entry.ExtractToFile(FindUniqueFileName(outputDir, Path.GetFileName(entry.Name)).FullName);
						}
					}
				}
			} catch {
				tempHandle.Dispose(); // don't leak in case of exception.
				throw;
			}
			return tempHandle;
		}

		private FileInfo FindUniqueFileName(DirectoryInfo dir, string filename) {
			lock (sync) {
				var file = new FileInfo(Path.Combine(dir.FullName, filename));
				int i = 0;
				while (file.Exists) {
					file = new FileInfo(Path.Combine(dir.FullName, $"{Path.GetFileNameWithoutExtension(filename)}_{i}_{Path.GetExtension(filename)}"));
				}
				return file;
			}
		}

		private DirectoryInfo FindUniqueTempDir(string dirname) {
			lock (sync) {
				var dir = new DirectoryInfo(Path.Combine(tempDir.FullName, dirname));
				int i = 0;
				while (dir.Exists || File.Exists(dir.FullName)) {
					dir = new DirectoryInfo($"{dir.FullName}_{i}");
				}
				dir.Create();
				return dir;
			}
		}
	}
}
