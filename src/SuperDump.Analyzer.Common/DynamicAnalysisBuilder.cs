using SuperDump.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SuperDump.Analyzer.Common {
	public class DynamicAnalysisBuilder {
		private readonly DynamicAnalysis analysis;

		public DynamicAnalysisBuilder(SDResult result, params DynamicAnalyzer[] analyzers) {
			analysis = new DynamicAnalysis(result);
			foreach (var analyzer in analyzers) {
				analysis.AddAnalyzer(analyzer);
			}
		}

		public DynamicAnalysisBuilder Add(params DynamicAnalyzer[] analyzers) {
			foreach (var analyzer in analyzers) {
				analysis.AddAnalyzer(analyzer);
			}
			return this;
		}

		public void Analyze() {
			analysis.Analyze();
		}
	}
}
