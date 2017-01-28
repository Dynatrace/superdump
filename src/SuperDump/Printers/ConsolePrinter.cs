using System;

namespace SuperDump.Printers {
	public class ConsolePrinter : BasePrinter {
		private class ConsoleColorChanger : IDisposable {
			private ConsoleColor _oldColor;

			public ConsoleColorChanger(ConsoleColor foregroundColor) {
				_oldColor = Console.ForegroundColor;
				Console.ForegroundColor = foregroundColor;
			}

			public void Dispose() {
				Console.ForegroundColor = _oldColor;
			}
		}

		public override void Write(string value) {
			Console.Write(value);
		}

		public override void WriteLine(string value) {
			Console.WriteLine(value);
		}

		public override void WriteError(string value) {
			using (new ConsoleColorChanger(ConsoleColor.Red)) {
				Console.WriteLine("ERROR: " + value);
			}
		}

		public override void WriteInfo(string value) {
			using (new ConsoleColorChanger(ConsoleColor.Cyan)) {
				Console.WriteLine("INFO: " + value);
			}
		}

		public override void WriteWarning(string value) {
			using (new ConsoleColorChanger(ConsoleColor.Yellow)) {
				Console.WriteLine("WARNING: " + value);
			}
		}
	}
}
