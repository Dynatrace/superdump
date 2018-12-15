using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SuperDump.Models;
using SuperDumpService.Models;

namespace SuperDumpService.Services {
	public interface IDumpStorage {
		Task<FileInfo> AddFileCopy(string bundleId, string dumpId, FileInfo sourcePath);
		void Create(string bundleId, string dumpId);
		void DeleteDumpFile(string bundleId, string dumpId);
		string GetDumpFilePath(string bundleId, string dumpId);
		FileInfo GetFile(string bundleId, string dumpId, string filename);
		IEnumerable<SDFileInfo> GetSDFileInfos(string bundleId, string dumpId);
		bool MiniInfoExists(DumpIdentifier id);
		Task<IEnumerable<DumpMetainfo>> ReadDumpMetainfoForBundle(string bundleId);
		Task<DumpMiniInfo> ReadMiniInfo(DumpIdentifier id);
		Task<SDResult> ReadResults(string bundleId, string dumpId);
		Task<SDResult> ReadResultsAndThrow(string bundleId, string dumpId);
		void Store(DumpMetainfo dumpInfo);
		Task StoreMiniInfo(DumpIdentifier id, DumpMiniInfo miniInfo);
		void WriteResult(DumpIdentifier id, SDResult result);
	}
}