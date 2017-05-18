using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CoreDumpAnalysis {
	public class ProcessHelper : IProcessHelper {
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
			process.Start();
			return process.StandardOutput;
		}
	}
}
