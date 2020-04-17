using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SuperDumpService.Models;

namespace SuperDumpService.Services.Analyzers {
	public enum AnalyzerState {
		/// <summary>
		/// No Further Analyzers should be executed.
		/// </summary>
		Cancelled,

		/// <summary>
		/// A primary dump was sucessfully analyzed. Further Pipeline Steps are executed.
		/// </summary>
		Succeeded,

		/// <summary>
		/// No primary dump was analyzed sucessfully. Further Pipeline Steps are executed.
		/// </summary>
		Failed,

		/// <summary>
		/// Indicates the first Analysis step.
		/// </summary>
		Initialized
	}

	/// <summary>
	/// This class defines a step in the analysis pipeline that enriches the result created by InitialAnalyzers.
	/// </summary>
	public abstract class AnalyzerJob {
		public abstract Task<AnalyzerState> AnalyzeDump(DumpMetainfo dumpInfo, string analysisWorkingDir, AnalyzerState previousState);
	}

	/// <summary>
	/// An InitialAnalyzer is a step in the analysis pipeline that can create new DumpMetainfo objects.
	/// Each DumpMetainfo object is then analyzed seperately.
	/// 
	/// This class should be used for main analysis steps, e.g. the analyzer creates a result that can be shown to the user on its own.
	/// </summary>
	public abstract class InitalAnalyzerJob : AnalyzerJob {
		public abstract Task<IEnumerable<DumpMetainfo>> CreateDumpInfos(string bundleId, DirectoryInfo directory);
	}

	/// <summary>
	/// This analyzer steps depend on the sucessful analysis of previous steps. These steps are only executed if the main analyis was sucessful.
	/// </summary>
	public abstract class PostAnalysisJob {
		public abstract Task AnalyzeDump(DumpMetainfo dumpInfo);
	}
}
