using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SuperDump.Models;

namespace SuperDumpService.Models {
	public class DumpMiniInfo {
		public int DumpSimilarityInfoVersion { get; set; }
		public ThreadMiniInfo FaultingThread { get; set; }
		public SDLastEvent LastEvent { get; set; }
		public ExceptionMiniInfo Exception { get; set; }

	}

	public class ThreadMiniInfo {
		public string[] DistinctModules { get; set; }
		public string[] DistrinctFrames { get; set; }
	}

	public class ExceptionMiniInfo {
		public string Type { get; set; }
		public string Message { get; set; }
	}
}
