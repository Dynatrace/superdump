using SuperDumpService.Models;
using System;
using System.Collections.Generic;

namespace SuperDumpService.ViewModels {
	public class DumpListViewModel {
		public DumpMetainfo DumpInfo { get; set; }
		public Similarities Similarities { get; set; }

		public DumpListViewModel(DumpMetainfo DumpInfo, Similarities similarities) {
			this.DumpInfo = DumpInfo;
			this.Similarities = similarities;
		}
	}
}
