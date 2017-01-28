using System.IO;

namespace SuperDump.Printers {
	public class FilePrinter : BasePrinter {
		private StreamWriter fileWriter;

		public FilePrinter(string fileName) {
			this.fileWriter = new StreamWriter(File.Open(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite));
			this.fileWriter.AutoFlush = true;
		}

		public override void Write(string value) {
			fileWriter.Write(value);
		}

		public override void WriteError(string value) {
			fileWriter.WriteLine("ERROR: " + value);
		}

		public override void WriteInfo(string value) {
			fileWriter.WriteLine("INFO: " + value);
		}

		public override void WriteLine(string value) {
			fileWriter.WriteLine(value);
		}

		public override void WriteWarning(string value) {
			fileWriter.WriteLine("WARNING: " + value);
		}

		public override void Dispose() {
			this.fileWriter.Flush();
			this.fileWriter.Dispose();
		}
	}
}