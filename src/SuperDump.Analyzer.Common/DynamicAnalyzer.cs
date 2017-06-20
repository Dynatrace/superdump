using SuperDump.Models;

namespace SuperDump.Analyzer.Common {
	public class DynamicAnalyzer {
		public virtual void AnalyzeModule(SDModule module) {
		}

		public virtual void AnalyzeThread(SDThread thread) {
		}

		public virtual void AnalyzeFrame(SDThread thread, SDCombinedStackFrame frame) {
		}

		public virtual void AnalyzeResult(SDResult result) {
		}

		protected bool ContainsAny(string stringToSearch, params string[] keys) {
			string stringToSearchLower = stringToSearch.ToLower();
			foreach (string key in keys) {
				if (stringToSearchLower.Contains(key.ToLower())) {
					return true;
				}
			}
			return false;
		}
	}
}
