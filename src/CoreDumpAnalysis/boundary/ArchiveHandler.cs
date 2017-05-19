using SharpCompress.Archives;
using SharpCompress.Archives.GZip;
using SharpCompress.Archives.Tar;
using SharpCompress.Archives.Zip;
using SharpCompress.Readers;
using System;
using System.IO;
using System.Linq;

namespace CoreDumpAnalysis {
	public class ArchiveHandler : IArchiveHandler {

		private readonly IFilesystem filesystem;

		public ArchiveHandler(IFilesystem filesystem) {
			this.filesystem = filesystem;
		}

		public bool TryExtract(String file) {
			if (file.EndsWith(".zip")) {
				using (var archive = ZipArchive.Open(file)) {
					Console.WriteLine("Extracting ZIP archive " + file);
					ExtractArchiveTo(archive, filesystem.GetParentDirectory(file));
				}
				File.Delete(file);
				return true;
			} else if (file.EndsWith(".gz")) {
				using (var archive = GZipArchive.Open(file)) {
					Console.WriteLine("Extracting GZ archive " + file);
					ExtractSingleEntryToFile(archive, file.Substring(0, file.Length - 3));
				}
				File.Delete(file);
				return true;
			} else if (file.EndsWith(".tar")) {
				using (var archive = TarArchive.Open(file)) {
					Console.WriteLine("Extracting TAR archive " + file);
					ExtractArchiveTo(archive, filesystem.GetParentDirectory(file));
				}
				File.Delete(file);
				return true;
			}
			return false;
		}

		private void ExtractArchiveTo(SharpCompress.Archives.IArchive archive, string parentDirectory) {
			foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory)) {
				entry.WriteToDirectory(parentDirectory, new ExtractionOptions() {
					ExtractFullPath = true,
					Overwrite = true
				});
			}
		}

		private void ExtractSingleEntryToFile(SharpCompress.Archives.IArchive archive, string file) {
			var entry = archive.Entries.Single();
			entry.WriteToFile(file, new ExtractionOptions() {
				ExtractFullPath = true,
				Overwrite = true
			});
		}
	}
}
