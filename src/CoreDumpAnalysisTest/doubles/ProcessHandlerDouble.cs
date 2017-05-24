using System.IO;
using CoreDumpAnalysis;
using System.Collections.Generic;
using System.Text;
using CoreDumpAnalysis.boundary;
using System;

namespace CoreDumpAnalysisTest {
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

		public StreamReader StartProcessAndRead(string fileName, string arguments) {
			return new StreamReader(MemoryStreamFromString(fileNameToOutputMap[fileName] ?? ""));
		}

		public ProcessStreams StartProcessAndReadWrite(string fileName, string arguments) {
			return new ProcessStreams(new StreamReader(MemoryStreamFromDict(fileNameToOutputMap, fileName)),
				new StreamWriter(MemoryStreamFromDict(fileNameToInputMap, fileName), Encoding.ASCII, 512, true),
				new StreamReader(MemoryStreamFromDict(fileNameToErrorMap, fileName)));
		}

		private MemoryStream MemoryStreamFromDict(IDictionary<string, string> dict, string key) {
			return MemoryStreamFromString(dict.TryGetValue(key, out string v) ? v : "");
		}

		private MemoryStream MemoryStreamFromString(string content) {
			return new MemoryStream(Encoding.UTF8.GetBytes(content));
		}
	}
}