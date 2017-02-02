using Hangfire;
using SuperDump.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SuperDumpService.Models {
	public interface IDumpRepository {
		[Queue("bundles", Order = 1)]
		Task AddBundle(IJobCancellationToken token, DumpBundle bundle);
		[Queue("analysis", Order = 2)]
		void AddDump(IJobCancellationToken token, DumpAnalysisItem item);
		IEnumerable<DumpBundle> GetAll();
		DumpBundle GetBundle(string bundleId);
		DumpAnalysisItem GetDump(string bundleid, string dumpId);
		DumpAnalysisItem GetDump(string dumpId);
		IEnumerable<string> GetReportFileNames(string bundleId, string id);
		FileInfo GetReportFile(string bundleid, string id, string filename);

		bool ContainsBundle(string bundleId);
		bool ContainsDump(string bundleId, string id);
		bool ContainsDump(string dumpId);
		void AddResult(string bundleId, string id, string resultPath);
		SDResult GetResult(string bundleId, string id);
		void DeleteBundle(string bundleId);
		void DeleteDump(string bundleId, string id);

		string CreateUniqueBundleId();

		string CreateUniqueDumpId();
		void WipeAllExceptDump(string bundleId, string dumpId);
	}
}
