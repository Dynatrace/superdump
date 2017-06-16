using SharpCompress.Archives;
using SharpCompress.Archives.GZip;
using SharpCompress.Archives.Tar;
using SharpCompress.Archives.Zip;
using SharpCompress.Readers;
using SuperDump.Common;
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
				// Using tar command because SharpCompress is unable to extract symbolic links
				ProcessRunner.Run("tar", new DirectoryInfo(Directory.GetCurrentDirectory()), "-hxf", file.FullName).Wait();
				file.Delete();
				return true;
			}
			return false;
		}

		private void ExtractArchiveTo(IArchive archive, string parentDirectory) {
			foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory)) {
				try {
					entry.WriteToDirectory(parentDirectory, new ExtractionOptions() {
						ExtractFullPath = true,
						Overwrite = true
					});
				} catch(Exception e) {
					Console.WriteLine($"Failed to extract {archive.Type.ToString()} archive: ${entry.Key}");
					Console.WriteLine(e.StackTrace);
				}
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
