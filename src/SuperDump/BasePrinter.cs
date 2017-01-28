using SuperDump.Printers;

namespace SuperDump {
	public abstract class BasePrinter : IPrinter {
		public void Write(string format, params object[] args) {
			Write(string.Format(format, args));
		}

		public void WriteLine(string format, params object[] args) {
			WriteLine(string.Format(format, args));
		}

		public void WriteInfo(string format, params object[] args) {
			WriteInfo(string.Format(format, args));
		}

		public void WriteError(string format, params object[] args) {
			WriteError(string.Format(format, args));
		}

		public void WriteWarning(string format, params object[] args) {
			WriteWarning(string.Format(format, args));
		}

		public abstract void Write(string value);
		public abstract void WriteLine(string value);
		public abstract void WriteInfo(string value);
		public abstract void WriteError(string value);
		public abstract void WriteWarning(string value);

		public virtual void Dispose() {
			// dispose action for each derived type, if neccessary!
		}
	}
}
