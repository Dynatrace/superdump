using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;
using System;

namespace SuperDump.Analyzers {
	public class WinDbgAnalyzer {
		private DumpContext context;
		private string logfilepath;

		public WinDbgAnalyzer(DumpContext context, string logfilepath) {
			this.context = context;
			this.logfilepath = logfilepath;
		}

		public void Analyze() {
			using (DataTarget t = context.CreateTemporaryDbgEngTarget()) {
				this.Analyze((IDebugControl6)t.DebuggerInterface, logfilepath);
			}
		}

		private void Analyze(IDebugControl6 debugControl, string logfilepath) {
			debugControl.OpenLogFile(logfilepath, false);
			try {
				LoadExtensions(debugControl);

				ExecuteCommand(debugControl, "|.", "status about process");
				//ExecuteCommand(debugControl, ".loadby sos clr", "try to find sos"); // this will load sos.dll from the location of clr.dll. if symbol servers work correctly, this is not what we need
				ExecuteCommand(debugControl, ".cordll -ve -u -l", "verbose loading of mscordacwks");
				ExecuteCommand(debugControl, ".chain", "list loaded debug-extensions");
				ExecuteCommand(debugControl, "!eeversion"); // clr version
				ExecuteCommand(debugControl, "!threads");
				ExecuteCommand(debugControl, "!tp", "threadpool info");
				ExecuteCommand(debugControl, "lmf"); // list loaded .dlls
				ExecuteCommand(debugControl, ".exr -1"); // last exception
				ExecuteCommand(debugControl, "!analyze -v");
				ExecuteCommand(debugControl, "~* k", "native stacks");
				ExecuteCommand(debugControl, "~*e !clrstack", "managed stacks");
				ExecuteCommand(debugControl, "x *!", "symbol paths"); // only shows symbols of modules that have been requested, so it's important to do this after listing stacks
			} finally {
				debugControl.CloseLogFile();
			}
		}

		private void ExecuteCommand(IDebugControl6 debugControl, string command, string comment = null) {
			debugControl.Execute(DEBUG_OUTCTL.ALL_CLIENTS, " ", DEBUG_EXECUTE.DEFAULT); // empty new line
			Echo(debugControl, $"now running command '{command}'" + (string.IsNullOrEmpty(comment) ? string.Empty : $" ({comment})"));
			Echo(debugControl, "=======================================================================");
			LogOnErrorHR(debugControl.Execute(DEBUG_OUTCTL.ALL_CLIENTS, command, DEBUG_EXECUTE.DEFAULT), $"failed to run '{command}'");
		}

		private static void Echo(IDebugControl6 debugControl, string msg) {
			LogOnErrorHR(debugControl.Execute(DEBUG_OUTCTL.ALL_CLIENTS, $"$$ {msg}", DEBUG_EXECUTE.DEFAULT), $"failed to " + $"$$ {msg}");
		}

		private static void LoadExtensions(IDebugControl6 debugControl) {
			if (Environment.Is64BitProcess) {
				LoadExtension(debugControl, @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\winext\ext.dll"); // do we need this configurable? should we ship these?
				LoadExtension(debugControl, @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\WINXP\exts.dll");
				LoadExtension(debugControl, @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\WINXP\uext.dll");
				LoadExtension(debugControl, @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\WINXP\ntsdexts.dll");
			} else {
				LoadExtension(debugControl, @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x86\winext\ext.dll"); // do we need this configurable? should we ship these?
				LoadExtension(debugControl, @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x86\WINXP\exts.dll");
				LoadExtension(debugControl, @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x86\WINXP\uext.dll");
				LoadExtension(debugControl, @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x86\WINXP\ntsdexts.dll");
			}
		}

		private static ulong LoadExtension(IDebugControl6 debugControl, string path) {
			ulong handle;
			LogOnErrorHR(debugControl.AddExtension("\"" + path + "\"", 0, out handle), $"failed to load '{path}'");
			return handle;
		}

		private static void LogOnErrorHR(int hr, string msg) {
			if (hr != 0) {
				Console.WriteLine($"HRESULT={hr}, {msg}");
			}
		}
	}
}
