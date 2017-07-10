using System;
using System.Collections.Generic;
using System.Text;

namespace SuperDump.Common {
	public sealed class LinuxAnalyzerExitCode {

		private static readonly Dictionary<int, LinuxAnalyzerExitCode> instance = new Dictionary<int, LinuxAnalyzerExitCode>();

		public readonly int Code;
		public readonly string Message;

		public LinuxAnalyzerExitCode(int code, string message) {
			this.Code = code;
			this.Message = message;
			instance[code] = this;
		}

		public static readonly LinuxAnalyzerExitCode Success = new LinuxAnalyzerExitCode(0, "");
		public static readonly LinuxAnalyzerExitCode InvalidArguments = new LinuxAnalyzerExitCode(2, "Invalid analyzation call. Please verify the call arguments in the SuperDump settings.");
		public static readonly LinuxAnalyzerExitCode NoCoredumpFound = new LinuxAnalyzerExitCode(3, "Could not find a coredump.");
		public static readonly LinuxAnalyzerExitCode FileNoteMissing = new LinuxAnalyzerExitCode(4, "Cannot analyze dumps without NT_FILE note. This note is only present in more recent kernel releases.");
		public static readonly LinuxAnalyzerExitCode UnknownCode = new LinuxAnalyzerExitCode(-1, "An unknown error occurred during the analysis. Please check the log files for more details.");

		public static explicit operator LinuxAnalyzerExitCode(int code) {
			if (instance.TryGetValue(code, out LinuxAnalyzerExitCode result)) {
				return result;
			} else {
				return UnknownCode;
			}
		}
	}
}
