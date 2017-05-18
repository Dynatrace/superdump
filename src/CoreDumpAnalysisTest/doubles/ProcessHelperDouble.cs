using System;
using System.IO;
using CoreDumpAnalysis;
using System.Collections.Generic;
using System.Text;

namespace CoreDumpAnalysisTest {
	internal class ProcessHelperDouble : IProcessHelper {
		private readonly Dictionary<string, string> fileNameToOutputMap = new Dictionary<string, string>();

		public void SetOutputForCommand(string command, string outputString) {
			fileNameToOutputMap.Add(command, outputString);
		}

		public StreamReader StartProcessAndRead(string fileName, string arguments) {
			return new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(fileNameToOutputMap[fileName])));
		}
	}
}