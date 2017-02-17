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
		private readonly PathHelper pathHelper;

		public UnpackService(PathHelper pathHelper) {
			this.pathHelper = pathHelper;
		}

		public DirectoryInfo UnZip(FileInfo file) {
			var outputDir = FindUniqueTempDir(file.Directory, Path.GetFileNameWithoutExtension(file.Name));
			ZipFile.ExtractToDirectory(file.FullName, outputDir.FullName);
			return outputDir;
		}

		private DirectoryInfo FindUniqueTempDir(DirectoryInfo dir, string dirname) {
			var subdir = new DirectoryInfo(Path.Combine(dir.FullName, dirname));
			int i = 0;
			while (subdir.Exists || File.Exists(subdir.FullName)) {
				subdir = new DirectoryInfo($"{subdir.FullName}_{i}");
			}
			subdir.Create();
			return subdir;
		}
	}
}
