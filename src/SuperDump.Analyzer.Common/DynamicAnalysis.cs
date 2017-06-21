using SuperDump.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SuperDump.Analyzer.Common {
	public partial class DynamicAnalysis {

		private readonly SDResult res;
		private readonly ISet<DynamicAnalyzer> analyzers = new HashSet<DynamicAnalyzer>();

		public DynamicAnalysis(SDResult res) {
			this.res = res;
		}

		public void AddAnalyzer(DynamicAnalyzer analyzer) {
			analyzers.Add(analyzer);
		}

		public void Analyze() {
			AnalyzeModules();
			AnalyzeThreads();
			AnalyzeSDResult();
		}

		private void AnalyzeModules() {
			foreach (var module in res.SystemContext.Modules) {
				foreach (DynamicAnalyzer analyzer in analyzers) {
					analyzer.AnalyzeModule(module);
				}
			}
		}

		private void AnalyzeThreads() {
			foreach (var thread in res.ThreadInformation.Values) {
				foreach (DynamicAnalyzer analyzer in analyzers) {
					analyzer.AnalyzeThread(thread);
				}
				foreach (var frame in thread.StackTrace) {
					foreach (DynamicAnalyzer analyzer in analyzers) {
						analyzer.AnalyzeFrame(thread, frame);
					}
				}
			}
		}
		
		private void AnalyzeSDResult() {
			foreach (DynamicAnalyzer analyzer in analyzers) {
				analyzer.AnalyzeResult(res);
			}
		}
	}
}
