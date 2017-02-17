

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SuperDumpService.Helpers {
	public class ProcessRunner : IDisposable {
		private readonly Process process;

		public string StdOut { get; private set; }
		public string StdErr { get; private set; }
		public int ExitCode { get; private set; }

		public ProcessRunner(string executable, DirectoryInfo workingDir, params string[] arguments) {
			this.process = new Process();
			this.process.StartInfo.FileName = executable;
			this.process.StartInfo.WorkingDirectory = workingDir.FullName;
			this.process.StartInfo.Arguments = string.Join(" ", arguments);
			this.process.StartInfo.RedirectStandardOutput = true;
			this.process.StartInfo.RedirectStandardError = true;
			this.process.StartInfo.UseShellExecute = false;
			this.process.StartInfo.CreateNoWindow = true;
		}

		public async Task<ProcessRunner> Start() {
			await Task.Run(() => {
				process.Start();
				TrySetPriorityClass(process, ProcessPriorityClass.BelowNormal);
				StdOut = process.StandardOutput.ReadToEnd(); // important to do ReadToEnd before WaitForExit to avoid deadlock
				StdErr = process.StandardError.ReadToEnd();
				process.WaitForExit();
				ExitCode = process.ExitCode;
			});
			return this;
		}

		private static void TrySetPriorityClass(Process process, ProcessPriorityClass priority) {
			try {
				process.PriorityClass = priority;
			} catch (Exception) {
				// this might be disallowed, e.g. in Azure WebApps
			}
		}

		public async static Task<ProcessRunner> Run(string executable, DirectoryInfo workingDir, params string[] arguments) {
			return await new ProcessRunner(executable, workingDir, arguments).Start();
		}

		public void Dispose() {
			this.process.Dispose();
		}
	}
}
