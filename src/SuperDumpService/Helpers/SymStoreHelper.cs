using System;
using System.Diagnostics;

namespace SuperDumpService.Helpers {
	public class SymStoreHelper {
		public string localSymbolCachePath;
		private string symStoreExex64;
		private string symStoreExex86;

		public SymStoreHelper(string localSymbolCachePath, string symStoreExex64, string symStoreExex86) {
			this.localSymbolCachePath = localSymbolCachePath;
			this.symStoreExex64 = symStoreExex64;
			this.symStoreExex86 = symStoreExex86;
		}

		public bool AddToSymStore(string pdbOrDllPath, Architecture arch = Architecture.x64) {
			try {
				var startInfo = new ProcessStartInfo() {
					FileName = GetSymStoreExe(arch),

					// add /f "dtagentcore.pdb" /s "D:\symbols" /t "uploaded symbol" /c "from superdump"
					Arguments = $"add /f \"{pdbOrDllPath}\" /s \"{localSymbolCachePath}\" /t \"uploaded-by-superdump\"",
					UseShellExecute = false,
					RedirectStandardOutput = true
				};
				using (var p = Process.Start(startInfo)) {
					p.WaitForExit();
					int exitcode = p.ExitCode;
					if (exitcode != 0) {
						Console.WriteLine($"symstore failed with exit code '{exitcode}' for '{pdbOrDllPath}'. command was '\"{startInfo.FileName}\" {startInfo.Arguments}'");

						return false;
					}
					Console.WriteLine($"successfully added '{pdbOrDllPath}' to local symbol cache");
				}
				return true;
			} catch (Exception e) {
				Console.WriteLine($"AddToSymStore failed: {e}");
				return false;
			}
		}

		private string GetSymStoreExe(Architecture arch) {
			if (arch == Architecture.x64) return symStoreExex64;
			if (arch == Architecture.x86) return symStoreExex86;
			throw new NotImplementedException();
		}
	}

	public enum Architecture {
		x86,
		x64
	}
}
