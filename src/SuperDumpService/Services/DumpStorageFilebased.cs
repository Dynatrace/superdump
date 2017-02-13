﻿using Newtonsoft.Json;
using SuperDumpService.Models;
using SuperDumpService.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SuperDump.Models;

namespace SuperDumpService.Services {
	/// <summary>
	/// for writing and reading of dumps only
	/// this implementation uses simple filebased storage
	/// </summary>
	public class DumpStorageFilebased {
		private readonly PathHelper pathHelper;

		public DumpStorageFilebased(PathHelper pathHelper) {
			this.pathHelper = pathHelper;
		}

		public IEnumerable<DumpMetainfo> ReadDumpMetainfoForBundle(string bundleId) {
			foreach (var dir in Directory.EnumerateDirectories(pathHelper.GetBundleDirectory(bundleId))) {
				var dumpId = new DirectoryInfo(dir).Name;
				var metainfoFilename = pathHelper.GetDumpMetadataPath(bundleId, dumpId);
				if (!File.Exists(metainfoFilename)) {
					// backwards compatibility, when Metadata files did not exist. read full json, then store metadata file
					CreateMetainfoForCompat(bundleId, dumpId);
				}
				yield return ReadMetainfoFile(metainfoFilename);
			}
		}

		private DumpMetainfo ReadMetainfoFile(string filename) {
			return JsonConvert.DeserializeObject<DumpMetainfo>(File.ReadAllText(filename));
		}

		private DumpMetainfo ReadMetainfoFile(string bundleId, string dumpId) {
			return ReadMetainfoFile(pathHelper.GetDumpMetadataPath(bundleId, dumpId));
		}

		private void WriteMetainfoFile(DumpMetainfo metaInfo, string filename) {
			File.WriteAllText(filename, JsonConvert.SerializeObject(metaInfo, Formatting.Indented));
		}

		private void CreateMetainfoForCompat(string bundleId, string dumpId) {
			var metainfo = new DumpMetainfo() {
				BundleId = bundleId,
				DumpId = dumpId
			};
			var result = ReadResults(bundleId, dumpId);
			if (result != null) {
				metainfo.Status = DumpStatus.Finished;
				metainfo.DumpFileName = result.AnalysisInfo.Path?.Replace(pathHelper.GetUploadsDir(), ""); // AnalysisInfo.FileName used to store full file names. e.g. "C:\superdump\uploads\myzipfilename\subdir\dump.dmp". lets only keep "myzipfilename\subdir\dump.dmp"
				metainfo.Created = result.AnalysisInfo.ServerTimeStamp;
			} else {
				metainfo.Status = DumpStatus.Failed;
			}

			WriteMetainfoFile(metainfo, pathHelper.GetDumpMetadataPath(bundleId, dumpId));
		}

		public SDResult ReadResults(string bundleId, string dumpId) {
			var filename = pathHelper.GetJsonPath(bundleId, dumpId);
			if (!File.Exists(filename)) return null;
			try {
				return JsonConvert.DeserializeObject<SDResult>(File.ReadAllText(filename));
			} catch (Exception e) {
				Console.WriteLine($"could not deserialize {filename}: {e.Message}");
				return null;
			}
		}

		public string GetDumpFilePath(string bundleId, string dumpId) {
			var filename = pathHelper.GetDumpfilePath(bundleId, dumpId);
			if (!File.Exists(filename)) return null;
			return filename;
		}

		/// <summary>
		/// actually copies a file into the dumpdirectory
		/// </summary>
		internal async Task<FileInfo> AddDumpFile(string bundleId, string dumpId, FileInfo sourcePath) {
			var destFile = new FileInfo(pathHelper.GetDumpfilePath(bundleId, dumpId));
			using (Stream source = sourcePath.OpenRead()) {
				using (Stream destination = destFile.Create()) {
					await source.CopyToAsync(destination);
				}
			}
			return destFile;
		}

		internal void Create(string bundleId, string dumpId) {
			string dir = pathHelper.GetDumpDirectory(bundleId, dumpId);
			if (Directory.Exists(dir)) {
				throw new DirectoryAlreadyExistsException("Cannot create '{dir}'. It already exists.");
			}
			Directory.CreateDirectory(dir);
		}

		internal void Store(DumpMetainfo dumpInfo) {
			WriteMetainfoFile(dumpInfo, pathHelper.GetDumpMetadataPath(dumpInfo.BundleId, dumpInfo.DumpId));
		}

		internal IEnumerable<SDFileInfo> GetSDFileInfos(string bundleId, string dumpId) {
			foreach (var filePath in Directory.EnumerateFiles(pathHelper.GetDumpDirectory(bundleId, dumpId))) {
				// in case the requested file has a "special" entry in FileEntry list, add that information
				var dumpInfo = ReadMetainfoFile(bundleId, dumpId);
				FileInfo fileInfo = new FileInfo(filePath);
				SDFileEntry fileEntry = GetSDFileEntry(dumpInfo, fileInfo);

				yield return new SDFileInfo() {
					FileInfo = fileInfo,
					FileEntry = fileEntry,
					SizeInBytes = fileInfo.Length
				};
			}
		}

		private SDFileEntry GetSDFileEntry(DumpMetainfo dumpInfo, FileInfo fileInfo) {
			// the file should be registered in dumpInfo
			SDFileEntry fileEntry = dumpInfo.Files.Where(x => x.FileName == fileInfo.Name).SingleOrDefault();
			if (fileEntry != null) return fileEntry;

			// but if it's not registered, do some heuristic to figure out which type of file it is.
			fileEntry = new SDFileEntry() {
				FileName = fileInfo.Name
			};
			if (Path.GetFileName(pathHelper.GetDumpfilePath(dumpInfo.BundleId, dumpInfo.DumpId)) == fileInfo.Name) {
				fileEntry.Type = SDFileType.PrimaryDump;
				return fileEntry;
			}
			if (Path.GetFileName(pathHelper.GetJsonPath(dumpInfo.BundleId, dumpInfo.DumpId)) == fileInfo.Name) {
				fileEntry.Type = SDFileType.SuperDumpData;
				return fileEntry;
			}
			if (Path.GetFileName(pathHelper.GetDumpMetadataPath(dumpInfo.BundleId, dumpInfo.DumpId)) == fileInfo.Name) {
				fileEntry.Type = SDFileType.SuperDumpData;
				return fileEntry;
			}
			if ("windbg.log" == fileInfo.Name) {
				fileEntry.Type = SDFileType.WinDbg;
				return fileEntry;
			}
			if (fileInfo.Extension == ".log") {
				fileEntry.Type = SDFileType.SuperDumpLogfile;
				return fileEntry;
			}
			if (fileInfo.Extension == ".json") {
				fileEntry.Type = SDFileType.SuperDumpData;
				return fileEntry;
			}
			if (fileInfo.Extension == ".dmp") {
				fileEntry.Type = SDFileType.PrimaryDump;
				return fileEntry;
			}

			// can't figure out filetype
			fileEntry.Type = SDFileType.Other;
			return fileEntry;
		}

		public FileInfo GetFile(string bundleId, string dumpId, string filename) {
			// make sure filename is not some relative ".."
			if (filename.Contains("..")) throw new UnauthorizedAccessException();

			string dir = pathHelper.GetDumpDirectory(bundleId, dumpId);
			var file = new FileInfo(Path.Combine(dir, filename));

			// make sure file really is inside of the dumps-directory
			if (!file.FullName.ToLower().Contains(dir.ToLower())) throw new UnauthorizedAccessException();

			if (file.Exists) {
				return file;
			} else {
				return null;
			}
		}
	}
}
