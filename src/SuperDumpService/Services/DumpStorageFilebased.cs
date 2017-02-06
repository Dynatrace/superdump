using Newtonsoft.Json;
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

		public DumpStorageFilebased() {

		}

		public IEnumerable<DumpMetainfo> ReadDumpMetainfoForBundle(string bundleId) {
			foreach (var dir in Directory.EnumerateDirectories(PathHelper.GetBundleDirectory(bundleId))) {
				var dumpId = new DirectoryInfo(dir).Name;
				var metainfoFilename = PathHelper.GetDumpMetadataPath(bundleId, dumpId);
				if (!File.Exists(metainfoFilename)) {
					// backwards compatibility, when Metadata files did not exist. read full json, then store metadata file
					CreateMetainfoForCompat(bundleId, dumpId);
				}
				yield return ReadMetainfoFile(metainfoFilename);
			}
		}

		private static DumpMetainfo ReadMetainfoFile(string filename) {
			return JsonConvert.DeserializeObject<DumpMetainfo>(File.ReadAllText(filename));
		}

		private static void WriteMetainfoFile(DumpMetainfo metaInfo, string filename) {
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
				metainfo.DumpFileName = result.AnalysisInfo.Path?.Replace(PathHelper.GetUploadsDir(), ""); // AnalysisInfo.FileName used to store full file names. e.g. "C:\superdump\uploads\myzipfilename\subdir\dump.dmp". lets only keep "myzipfilename\subdir\dump.dmp"
				metainfo.Created = result.AnalysisInfo.ServerTimeStamp;
			} else {
				metainfo.Status = DumpStatus.Failed;
			}

			WriteMetainfoFile(metainfo, PathHelper.GetDumpMetadataPath(bundleId, dumpId));
		}

		public SDResult ReadResults(string bundleId, string dumpId) {
			var filename = PathHelper.GetJsonPath(bundleId, dumpId);
			if (!File.Exists(filename)) return null;
			try {
				return JsonConvert.DeserializeObject<SDResult>(File.ReadAllText(filename));
			} catch (Exception e) {
				Console.WriteLine($"could not deserialize {filename}: {e.Message}");
				return null;
			}
		}

		public string GetDumpFilePath(string bundleId, string dumpId) {
			var filename = PathHelper.GetDumpfilePath(bundleId, dumpId);
			if (!File.Exists(filename)) return null;
			return filename;
		}

		/// <summary>
		/// actually copies a file into the dumpdirectory
		/// </summary>
		internal async Task<string> AddDumpFile(string bundleId, string dumpId, FileInfo sourcePath) {
			string destPath = PathHelper.GetDumpfilePath(bundleId, dumpId);
			using (Stream source = sourcePath.OpenRead()) {
				using (Stream destination = File.Create(destPath)) {
					await source.CopyToAsync(destination);
				}
			}
			return destPath;
		}

		internal void Create(string bundleId, string dumpId) {
			string dir = PathHelper.GetDumpDirectory(bundleId, dumpId);
			if (Directory.Exists(dir)) {
				throw new DirectoryAlreadyExistsException("Cannot create '{dir}'. It already exists.");
			}
			Directory.CreateDirectory(dir);
		}

		internal void Store(DumpMetainfo dumpInfo) {
			WriteMetainfoFile(dumpInfo, PathHelper.GetDumpMetadataPath(dumpInfo.BundleId, dumpInfo.DumpId));
		}

		internal IEnumerable<string> GetFileNames(string bundleId, string dumpId) {
			foreach (var file in Directory.EnumerateFiles(PathHelper.GetDumpDirectory(bundleId, dumpId))) {
				yield return new FileInfo(file).Name;
			}
		}

		public FileInfo GetFile(string bundleId, string dumpId, string filename) {
			// make sure filename is not some relative ".."
			if (filename.Contains("..")) throw new UnauthorizedAccessException();

			string dir = PathHelper.GetDumpDirectory(bundleId, dumpId);
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
