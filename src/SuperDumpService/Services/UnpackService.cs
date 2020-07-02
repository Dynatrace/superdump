using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

namespace SuperDumpService.Services {
	public enum ArchiveType {
		Zip,
		TarGz,
		Tar
	}

	public class UnpackService {
		private static readonly string invalidCharacters = Regex.Escape(
			new string(Path.GetInvalidFileNameChars().Where(c => c != Path.DirectorySeparatorChar).ToArray()));
		private static readonly Regex invalidCharacterRegex = new Regex($"[{invalidCharacters}]+");


		public static bool IsSupportedArchive(string filename) {
			return filename.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
				filename.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) && filename != "libs.tar.gz" ||
				filename.EndsWith(".tar", StringComparison.OrdinalIgnoreCase);
		}

		private static void ExtractZip(FileInfo file, DirectoryInfo outputDir) {
			using (ZipArchive zipArchive = ZipFile.OpenRead(file.FullName)) {
				foreach (ZipArchiveEntry entry in zipArchive.Entries) {
					string outName = Path.Combine(outputDir.FullName, RemoveInvalidChars(entry.FullName));
					Directory.CreateDirectory(Path.GetDirectoryName(outName));

					if (!Path.EndsInDirectorySeparator(outName)) {
						entry.ExtractToFile(outName);
					}
				}
			}
		}

		private static void ExtractTarGz(FileInfo file, DirectoryInfo outputDir) {
			using (FileStream inputStream = file.OpenRead()) {
				using (Stream gzipStream = new GZipInputStream(inputStream)) {
					ExtractTarStream(gzipStream, outputDir);
				}
			}
		}

		private static void ExtractTar(FileInfo file, DirectoryInfo outputDir) {
			using (FileStream inputStream = file.OpenRead()) {
				ExtractTarStream(inputStream, outputDir);
			}
		}

		private static void ExtractTarStream(Stream inputStream, DirectoryInfo outputDir) {
			using (var tarIn = new TarInputStream(inputStream)) {
				TarEntry tarEntry;
				while ((tarEntry = tarIn.GetNextEntry()) != null) {
					string entryName = tarEntry.Name;

					// Remove any root e.g. '\' because a PathRooted filename defeats Path.Combine
					if (Path.IsPathRooted(entryName))
						entryName = entryName.Substring(Path.GetPathRoot(entryName).Length);

					string outName = Path.Combine(outputDir.FullName, RemoveInvalidChars(entryName));
		
					if (tarEntry.IsDirectory) {
						Directory.CreateDirectory(outName);
					} else {
						Directory.CreateDirectory(Path.GetDirectoryName(outName));
						using (var outStr = new FileStream(outName, FileMode.Create)) {
							tarIn.CopyEntryContents(outStr);
						}
					}
				}
			}
		}

		private static string RemoveInvalidChars(string filename) {
			if (Path.DirectorySeparatorChar == '\\') {
				filename = filename.Replace('/', Path.DirectorySeparatorChar);
			} else {
				filename = filename.Replace('\\', Path.DirectorySeparatorChar);
			}

			return invalidCharacterRegex.Replace(filename, "_");
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
					ExtractZip(file, outputDir);
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
