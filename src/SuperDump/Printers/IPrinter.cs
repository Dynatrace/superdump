using System;

namespace SuperDump.Printers {
	public interface IPrinter : IDisposable {
		void Write(string value);
		void Write(string format, params object[] args);

		void WriteLine(string value);
		void WriteLine(string format, params object[] args);

		void WriteInfo(string value);
		void WriteInfo(string format, params object[] args);

		void WriteError(string value);
		void WriteError(string format, params object[] args);

		void WriteWarning(string value);
		void WriteWarning(string format, params object[] args);
	}
}
