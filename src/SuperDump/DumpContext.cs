using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;
using Microsoft.Diagnostics.Runtime.Utilities;
using SuperDump.Printers;
using System;

namespace SuperDump {
	public class DumpContext : IPrinter {
		public ClrRuntime Runtime { get; set; }
		public DataTarget Target { get; set; }
		public string DumpFile { get; set; }
		public string DumpDirectory { get; set; }
		public string DacLocation { get; set; }
		public ClrHeap Heap { get; set; }
		public SymbolLocator SymbolLocator { get; set; }
		public string SymbolPath { get; set; }
		public IPrinter Printer { get; set; }

		private DataTarget dbgEngTarget;

		public DumpContext() { }

		public bool IsInNativeMode => dbgEngTarget != null; // check if target exists

		public DataTarget CreateTemporaryDbgEngTarget() {
			if (dbgEngTarget != null)
				throw new InvalidOperationException("There is already a persistent DbgEng DataTarget. Creating a temporary one is not allowed.");

			return CreateDbgEngDataTargetImpl();
		}

		public DataTarget NativeDbgEngTarget => dbgEngTarget;

		private DataTarget CreateDbgEngDataTargetImpl() {
			if (String.IsNullOrEmpty(DumpFile)) {
				throw new InvalidOperationException("DbgEng targets only avaliable for dump files right now.");
			}

			DataTarget target = DataTarget.LoadCrashDump(DumpFile, CrashDumpReader.DbgEng);
			target.SymbolLocator.SymbolPath = SymbolPath;
			((IDebugSymbols2)target.DebuggerInterface).SetSymbolPath(SymbolPath);

			var outputCallbacks = new OutputCallbacks(this);
			var client = (IDebugClient5)target.DebuggerInterface;
			//Utility.CheckHRESULT(client.SetOutputCallbacks(outputCallbacks));

			return target;
		}

		public void EnterDbgEngNativeMode() {
			dbgEngTarget = CreateDbgEngDataTargetImpl();
		}

		public void ExitDbgEngNativeMode() {
			dbgEngTarget.Dispose();
			dbgEngTarget = null;
		}

		public void Dispose() {
			if (dbgEngTarget != null) {
				dbgEngTarget.Dispose();
			}
			if (Printer != null) {
				Printer.Dispose();
			}
			if (Target != null) {
				Target.Dispose();
			}
		}

		// IPrinter implementation
		public void Write(string value) {
			this.Printer.Write(value);
		}

		public void Write(string format, params object[] args) {
			this.Printer.Write(format, args);
		}

		public void WriteLine(string value) {
			this.Printer.WriteLine(value);
		}

		public void WriteLine(string format, params object[] args) {
			this.Printer.WriteLine(format, args);
		}

		public void WriteInfo(string value) {
			this.Printer.WriteInfo(value);
		}

		public void WriteInfo(string format, params object[] args) {
			this.Printer.WriteInfo(format, args);
		}

		public void WriteError(string value) {
			this.Printer.WriteError(value);
		}

		public void WriteError(string format, params object[] args) {
			this.Printer.WriteError(format, args);
		}

		public void WriteWarning(string value) {
			this.Printer.WriteWarning(value);
		}

		public void WriteWarning(string format, params object[] args) {
			this.Printer.WriteWarning(format, args);
		}
	}
}
