using SuperDump.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Thinktecture.IO;

namespace SuperDump.Analyzer.Linux.Boundary {
	public class ProcessHandler : IProcessHandler {
		public async Task<string> ExecuteProcessAndGetOutputAsync(string executable, string arguments) {
			using (var runner = await ProcessRunner.Run(executable, new DirectoryInfo(Directory.GetCurrentDirectory()), arguments)) {
				return runner.StdOut;
			}
		}

		public ProcessStreams StartProcessAndReadWrite(string executable, string arguments) {
			var process = new Process {
				StartInfo = new ProcessStartInfo {
					FileName = executable,
					Arguments = arguments,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardInput = true,
					RedirectStandardError = true,
					CreateNoWindow = true
				}
			};
			process.Start();
			return new ProcessStreams(process.StandardOutput, process.StandardInput, process.StandardError, () => process.Dispose());
		}
	}
}
