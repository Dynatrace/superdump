using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SuperDump.Common {
	public class ProcessRunner : IDisposable {
		private readonly Process process;

		public string StdOut { get; private set; }
		public string StdErr { get; private set; }
		public int ExitCode { get; private set; }

		public ProcessRunner(string executable, DirectoryInfo workingDir, params string[] arguments)
			: this(executable, workingDir, true, true, arguments) {
		}

		public ProcessRunner(string executable, DirectoryInfo workingDir, bool redirectOutput, bool redirectError, params string[] arguments) {
			this.process = new Process();
			this.process.StartInfo.FileName = executable;
			this.process.StartInfo.WorkingDirectory = workingDir.FullName;
			this.process.StartInfo.Arguments = string.Join(" ", arguments);
			this.process.StartInfo.RedirectStandardOutput = redirectOutput;
			this.process.StartInfo.RedirectStandardError = redirectError;
			this.process.StartInfo.UseShellExecute = false;
			this.process.StartInfo.CreateNoWindow = true;
		}

		public async Task<ProcessRunner> Start() {
			string info = $"starting process. exe: '{process.StartInfo.FileName}' {process.StartInfo.Arguments}, workdir: '{process.StartInfo.WorkingDirectory}'";
			Console.WriteLine(info);
			await Task.Run(() => {
				try {
					process.Start();
					TrySetPriorityClass(process, ProcessPriorityClass.BelowNormal);
					if (process.StartInfo.RedirectStandardOutput) {
						process.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e) { StdOut += e.Data + Environment.NewLine; };
						process.BeginOutputReadLine();
					}
					if (process.StartInfo.RedirectStandardError) {
						StdErr = process.StandardError.ReadToEnd();
					}
					process.WaitForExit();
					ExitCode = process.ExitCode;
				} catch (Exception e) {
					throw new ProcessRunnerException($"An exception occurred while starting a process: {info}", e);
				}
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

		public async static Task<ProcessRunner> RunWithoutRedirection(string executable, DirectoryInfo workingDir, params string[] arguments) {
			return await new ProcessRunner(executable, workingDir, false, false, arguments).Start();
		}

		public void Dispose() {
			this.process.Dispose();
		}
	}
}
