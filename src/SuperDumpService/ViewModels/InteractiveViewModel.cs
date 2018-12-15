using SuperDumpService.Models;
using System;
using System.Collections.Generic;

namespace SuperDumpService.ViewModels {
	public class InteractiveViewModel {
		public DumpIdentifier Id { get; set; }
		public DumpMetainfo DumpInfo { get; set; }
		public string Command { get; set; }
	}
}
