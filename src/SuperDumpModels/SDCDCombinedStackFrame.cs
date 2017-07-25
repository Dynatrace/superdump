using SuperDump.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SuperDumpModels {
	public class SDCDCombinedStackFrame : SDCombinedStackFrame {

		private static readonly Regex HexNumberRegex = new Regex("0x[0-9A-Fa-f]{2,}", RegexOptions.Compiled);

		public SDCDCombinedStackFrame(string moduleName, string methodName, ulong offsetInMethod, ulong ip, ulong sp, ulong returnOffset, ulong spOffset, SDFileAndLineNumber sourceInfo)
			: base(StackFrameType.Native, moduleName, methodName, offsetInMethod, ip, sp, returnOffset, spOffset, sourceInfo) {
		}

		public IDictionary<string, string> Locals { get; } = new Dictionary<string, string>();
		public IDictionary<string, string> Args { get; } = new Dictionary<string, string>();

		/// <summary>
		/// Split the values (= stack contents) by hex numbers. This is a dirty hack to enable gdb links in the UI.
		/// The number of items in the returned dictionary must always be uneven.
		/// </summary>
		public IDictionary<string, IEnumerable<string>> LocalsWithLinks {
			get {
				return SplitValuesByHexNumber(Locals);
			}
		}

		public IDictionary<string, IEnumerable<string>> ArgsWithLinks {
			get {
				return SplitValuesByHexNumber(Args);
			}
		}

		private IDictionary<string, IEnumerable<string>> SplitValuesByHexNumber(IDictionary<string, string> dict) {
			return dict.ToDictionary(kvp => kvp.Key, kvp => SplitByHexNumber(kvp.Value));
		}

		private IEnumerable<string> SplitByHexNumber(string s) {
			var list = new List<string>();
			if (s.Contains("0x")) {
				Match match = HexNumberRegex.Match(s);
				while (match.Success) {
					list.Add(s.Substring(0, match.Index));
					list.Add(s.Substring(match.Index, match.Length));
					s = s.Substring(match.Index + match.Length);

					match = HexNumberRegex.Match(s);
				}
			}
			list.Add(s);
			return list;
		}
	}
}
