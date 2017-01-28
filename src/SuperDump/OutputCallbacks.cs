using Microsoft.Diagnostics.Runtime.Interop;

namespace SuperDump {
	public class OutputCallbacks : IDebugOutputCallbacks {
		private DumpContext context;

		public OutputCallbacks(DumpContext context) {
			this.context = context;
		}

		public int Output(DEBUG_OUTPUT mask, string text) {
			switch (mask) {
				case DEBUG_OUTPUT.ERROR:
					context.WriteError(text.TrimEnd('\n', '\r'));
					break;
				case DEBUG_OUTPUT.EXTENSION_WARNING:
				case DEBUG_OUTPUT.WARNING:
					context.WriteWarning(text.TrimEnd('\n', '\r'));
					break;
				case DEBUG_OUTPUT.SYMBOLS:
					context.WriteInfo(text.TrimEnd('\n', '\r'));
					break;
				default:
					context.WriteLine(text);
					break;
			}

			return 0;
		}
	}
}
