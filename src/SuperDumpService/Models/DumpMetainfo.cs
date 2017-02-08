using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.Models {
	public class DumpMetainfo {
		public string BundleId { get; set; }
		public string DumpId { get; set; }
		public string DumpFileName { get; set; } // original filename. just informational.
		public DateTime Created { get; set; }
		public DateTime Finished { get; set; }
		public DumpStatus Status { get; set; }
		public string ErrorMessage { get; set; }
	}

	public enum DumpStatus {
		Created, Downloading, Analyzing, Finished,
		Failed
	}
}
