using System;
using System.Collections.Generic;
using System.Text;

namespace CoreDumpAnalysis {
	public static class Constants {
		public const string WRAPPER = "unwindwrapper.so";

		public const string DEBUG_SYMBOL_URL_PATTERN = "http://dbg-ruxit.dynatrace.vmta/{hash}/{file}";

		public const string DEBUG_SYMBOL_PATH = "../../../debug/";
	}
}
