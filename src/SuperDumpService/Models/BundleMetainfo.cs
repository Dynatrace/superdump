using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.Models {
	public class BundleMetainfo {
		public string BundleId { get; set; }
		public DateTime Created { get; set; }
		public DateTime Finished { get; set; }
		public BundleStatus Status { get; set; }
		public Dictionary<string, string> CustomProperties { get; set; } = new Dictionary<string, string>();
	}

	public enum BundleStatus {
		Created, Downloading, Analyzing, Finished
	}
}
