using SuperDumpService.Models;
using System;
using System.Collections.Generic;

namespace SuperDumpService.ViewModels {
	public class InteractiveViewModel {
		public string BundleId { get; set; }
		public string DumpId { get; set; }
		public DumpMetainfo DumpInfo { get; set; }
	}
}
