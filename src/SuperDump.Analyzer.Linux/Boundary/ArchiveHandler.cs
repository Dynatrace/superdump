using SharpCompress.Archives;
using SharpCompress.Archives.GZip;
using SharpCompress.Archives.Tar;
using SharpCompress.Archives.Zip;
using SharpCompress.Readers;
using System;
using System.IO;
using System.Linq;
using Thinktecture.IO;

namespace SuperDump.Analyzer.Linux.Boundary {
	public class ArchiveHandler : IArchiveHandler {

		private readonly IFilesystem filesystem;

		public ArchiveHandler(IFilesystem filesystem) {
			this.filesystem = filesystem;
		}

		public bool TryExtractAndDelete(IFileInfo file) {
			if (file.Extension == ".zip") {
				using (var archive = ZipArchive.Open(file.FullName)) {
					Console.WriteLine("Extracting ZIP archive " + file);
					ExtractArchiveTo(archive, file.DirectoryName);
				}
				file.Delete();
				return true;
			} else if (file.Extension == ".gz") {
				using (var archive = GZipArchive.Open(file.FullName)) {
					Console.WriteLine("Extracting GZ archive " + file);
					ExtractSingleEntryToFile(archive, Path.Combine(file.DirectoryName, Path.GetFileNameWithoutExtension(file.FullName)));
				}
				file.Delete();
				return true;
			} else if (file.Extension == ".tar") {
				using (var archive = TarArchive.Open(file.FullName)) {
					Console.WriteLine("Extracting TAR archive " + file);
					ExtractArchiveTo(archive, file.DirectoryName);
				}
				file.Delete();
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
