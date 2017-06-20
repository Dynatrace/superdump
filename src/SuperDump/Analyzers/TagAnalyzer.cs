using SuperDump.Analyzer;
using SuperDump.Analyzer.Common;
using SuperDump.Models;
using System.Collections.Generic;
using System.Linq;

namespace SuperDump.Analyzers {
	/// <summary>
	/// This analyzer only cares about putting special "Tags" onto "ITaggableItems".
	/// E.g. mark agent frames, exceptions, ...
	/// </summary>
	public class TagAnalyzer {
		private readonly SDResult res;

		public TagAnalyzer(SDResult res) {
			this.res = res;
		}

		public void Analyze() {
			var tagAnalyzer = new DynamicAnalysisBuilder(res, 
				new UniversalTagAnalyzer(), new DotNetTagAnalyzer(), new DynatraceTagAnalyzer(), new WindowsTagAnalyzer());
			tagAnalyzer.Analyze();
		}

		public static string TagsAsString(string prefix, IEnumerable<SDTag> tags) {
			if (!tags.Any()) return string.Empty;
			return prefix + "{" + string.Join(", ", tags) + "}";
		}
	}
}
