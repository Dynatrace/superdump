using CoreDumpAnalysis.boundary;
using System;
using System.Diagnostics;
using System.IO;

namespace CoreDumpAnalysis {
	public class ProcessHandler : IProcessHandler {
		public StreamReader StartProcessAndRead(string fileName, string arguments) {
			var process = new Process {
				StartInfo = new ProcessStartInfo {
					FileName = fileName,
					Arguments = arguments,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					CreateNoWindow = true
				}
			};
			try {
				process.Start();
			} catch(Exception e) {
				throw new ProcessStartFailedException(e);
			}
			return process.StandardOutput;
		}

		public ProcessStreams StartProcessAndReadWrite(string fileName, string arguments) {
			var process = new Process {
				StartInfo = new ProcessStartInfo {
					FileName = fileName,
					Arguments = arguments,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardInput = true,
					RedirectStandardError = true,
					CreateNoWindow = true
				}
			};
			process.Start();
			return new ProcessStreams(process.StandardOutput, process.StandardInput, process.StandardError);
		}
	}
}
