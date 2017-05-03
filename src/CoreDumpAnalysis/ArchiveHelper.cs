using SharpCompress.Archives;
using SharpCompress.Archives.GZip;
using SharpCompress.Archives.Tar;
using SharpCompress.Archives.Zip;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CoreDumpAnalysis
{
    class ArchiveHelper
    {
        public static bool TryExtract(String file)
        {
            if (file.EndsWith(".zip"))
            {
                using (var archive = ZipArchive.Open(file))
                {
                    Console.WriteLine("Extracting ZIP archive " + file);
                    ExtractArchiveTo(archive, FilesystemHelper.GetParentDirectory(file));
                }
                File.Delete(file);
                return true;
            }
            else if (file.EndsWith(".gz"))
            {
                using (var archive = GZipArchive.Open(file))
                {
                    Console.WriteLine("Extracting GZ archive " + file);
                    ExtractSingleEntryToFile(archive, file.Substring(0, file.Length - 3));
                }
                File.Delete(file);
                return true;
            }
            else if (file.EndsWith(".tar"))
            {
                using (var archive = TarArchive.Open(file))
                {
                    Console.WriteLine("Extracting TAR archive " + file);
                    ExtractArchiveTo(archive, FilesystemHelper.GetParentDirectory(file));
                }
                File.Delete(file);
                return true;
            }
            return false;
        }

        private static void ExtractArchiveTo(IArchive archive, string parentDirectory)
        {
            foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
            {
                entry.WriteToDirectory(parentDirectory, new ExtractionOptions()
                {
                    ExtractFullPath = true,
                    Overwrite = true
                });
            }
        }

        private static void ExtractSingleEntryToFile(IArchive archive, string file)
        {
            var entry = archive.Entries.Single();
            entry.WriteToFile(file, new ExtractionOptions()
            {
                ExtractFullPath = true,
                Overwrite = true
            });
        }
    }
}
