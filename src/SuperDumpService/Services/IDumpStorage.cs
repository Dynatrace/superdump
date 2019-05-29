using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SuperDump.Models;
using SuperDumpService.Models;

namespace SuperDumpService.Services {
	public interface IDumpStorage {
		Task<FileInfo> AddFileCopy(DumpIdentifier id, FileInfo sourcePath);
		void Create(DumpIdentifier id);
		void DeleteDumpFile(DumpIdentifier id);
		string GetDumpFilePath(DumpIdentifier id);
		string GetDumpFilePath(DumpMetainfo dumpInfo);
		FileInfo GetFile(DumpIdentifier id, string filename);
		IEnumerable<SDFileInfo> GetSDFileInfos(DumpIdentifier id);
		IEnumerable<SDFileInfo> GetSDFileInfos(DumpMetainfo dumpInfo);
		bool MiniInfoExists(DumpIdentifier id);
		Task<IEnumerable<DumpMetainfo>> ReadDumpMetainfoForBundle(string bundleId);
		Task<DumpMiniInfo> ReadMiniInfo(DumpIdentifier id);
		Task<SDResult> ReadResults(DumpIdentifier id);
		Task<SDResult> ReadResultsAndThrow(DumpIdentifier id);
		void Store(DumpMetainfo dumpInfo);
		Task StoreMiniInfo(DumpIdentifier id, DumpMiniInfo miniInfo);
		void WriteResult(DumpIdentifier id, SDResult result);
		bool ReadIsPrimaryDumpAvailable(DumpMetainfo dumpMetainfo);
	}
}