using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SuperDumpService.Models;

namespace SuperDumpService.Services.Analyzers {
	public enum AnalyzerState {
		/// <summary>
		/// No Further Analyzers should be executed.
		/// </summary>
		Cancel,

		/// <summary>
		/// A primary dump was sucessfully analyzed. Further Pipeline Steps are executed.
		/// </summary>
		Success,

		/// <summary>
		/// No primary dump was analyzed sucessfully. Further Pipeline Steps are executed.
		/// </summary>
		Failure,

		/// <summary>
		/// Indicates the first Analysis step.
		/// </summary>
		Initialized
	}

	public abstract class AnalyzerJob {
		public abstract Task<AnalyzerState> AnalyzeDump(DumpMetainfo dumpInfo, string analysisWorkingDir, AnalyzerState previousState);
	}

	public abstract class InitalAnalyzerJob : AnalyzerJob {
		public abstract Task<IEnumerable<DumpMetainfo>> CreateDumpInfos(string bundleId, DirectoryInfo directory);
	}
}
