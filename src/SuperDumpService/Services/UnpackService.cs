using System.IO;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

namespace SuperDumpService.Services {
	public enum ArchiveType {
		Zip,
		TarGz,
		Tar
	}

	public class UnpackService {

		private static void ExtractTarGz(FileInfo file, DirectoryInfo outputDir) {
			using (FileStream inputStream = file.OpenRead()) {
				using (Stream gzipStream = new GZipInputStream(inputStream)) {
					using (var tarArchive = TarArchive.CreateInputTarArchive(gzipStream)) {
						tarArchive.ExtractContents(outputDir.FullName);
					}
				}
			}
		}

		private static void ExtractTar(FileInfo file, DirectoryInfo outputDir) {
			using (FileStream inputStream = file.OpenRead()) {
				using (var tarArchive = TarArchive.CreateInputTarArchive(inputStream)) {
					tarArchive.ExtractContents(outputDir.FullName);
				}
			}
		}

		private static DirectoryInfo FindUniqueTempDir(DirectoryInfo dir, string dirname) {
			var subdir = new DirectoryInfo(Path.Combine(dir.FullName, dirname));
			int i = 0;
			while (subdir.Exists || File.Exists(subdir.FullName)) {
				subdir = new DirectoryInfo($"{subdir.FullName}_{i}");
			}
			subdir.Create();
			return subdir;
		}

		public DirectoryInfo ExtractArchive(FileInfo file, ArchiveType type) {
			DirectoryInfo outputDir = FindUniqueTempDir(file.Directory, Path.GetFileNameWithoutExtension(file.Name));
			switch (type) {
				case ArchiveType.Zip:
					ZipFile.ExtractToDirectory(file.FullName, outputDir.FullName);
					break;
				case ArchiveType.TarGz:
					ExtractTarGz(file, outputDir);
					break;
				case ArchiveType.Tar:
					ExtractTar(file, outputDir);
					break;
			}
			return outputDir;
		}
	}
}
