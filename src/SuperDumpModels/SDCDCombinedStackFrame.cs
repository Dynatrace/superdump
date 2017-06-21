using SuperDump.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpModels {
	public class SDCDCombinedStackFrame : SDCombinedStackFrame {
		public SDCDCombinedStackFrame(string moduleName, string methodName, ulong offsetInMethod, ulong ip, ulong sp, ulong returnOffset, ulong spOffset, SDFileAndLineNumber sourceInfo)
			: base(StackFrameType.Native, moduleName, methodName, offsetInMethod, ip, sp, returnOffset, spOffset, sourceInfo) { 
		}

		public IDictionary<string, string> Locals { get; } = new Dictionary<string, string>();
		public IDictionary<string, string> Args { get; } = new Dictionary<string, string>();

		public bool HasStackInfo() {
			return Locals.Count > 0 || Args.Count > 0;
		}
	}
}
