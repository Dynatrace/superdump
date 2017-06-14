using System.IO;
using SuperDump.Analyzer.Linux;
using System.Collections.Generic;
using System.Text;
using System;
using SuperDump.Analyzer.Linux.Boundary;
using System.Threading.Tasks;

namespace SuperDump.Analyzer.Linux.Test {
	internal class ProcessHandlerDouble : IProcessHandler {
		private readonly Dictionary<string, string> fileNameToOutputMap = new Dictionary<string, string>();
		private readonly Dictionary<string, string> fileNameToInputMap = new Dictionary<string, string>();
		private readonly Dictionary<string, string> fileNameToErrorMap = new Dictionary<string, string>();

		public void SetOutputForCommand(string command, string outputString) {
			fileNameToOutputMap.Add(command, outputString);
		}

		public void SetErrorForCommand(string command, string errorString) {
			fileNameToErrorMap.Add(command, errorString);
		}

		public Task<string> ExecuteProcessAndGetOutputAsync(string fileName, string arguments) {
			Task<string> t = new Task<string>(() => fileNameToOutputMap[fileName] ?? "");
			t.Start();
			return t;
		}

		public ProcessStreams StartProcessAndReadWrite(string fileName, string arguments) {
			return new ProcessStreams(new StreamReader(MemoryStreamFromDict(fileNameToOutputMap, fileName)),
				new StreamWriter(MemoryStreamFromDict(fileNameToInputMap, fileName), Encoding.ASCII, 512, true),
				new StreamReader(MemoryStreamFromDict(fileNameToErrorMap, fileName)), () => { });
		}

		private MemoryStream MemoryStreamFromDict(IDictionary<string, string> dict, string key) {
			return MemoryStreamFromString(dict.TryGetValue(key, out string v) ? v : "");
		}

		private MemoryStream MemoryStreamFromString(string content) {
			return new MemoryStream(Encoding.UTF8.GetBytes(content));
		}
	}
}