using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.Models {
	public class DumpMetainfo {
		public string BundleId { get; set; }
		public string DumpId { get; set; }
		public DateTime Created { get; set; }
		public DateTime Finished { get; set; }
		public DumpStatus Status { get; set; }
		public string ErrorMessage { get; internal set; }
	}

	public enum DumpStatus {
		Created, Downloading, Analyzing, Finished,
		Failed
	}
}
