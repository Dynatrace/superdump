using SuperDumpService.Models;
using System;
using System.Collections.Generic;

namespace SuperDumpService.ViewModels {
	public class DumpViewModel {
		public DumpMetainfo DumpInfo { get; set; }
		public Similarities Similarities { get; set; }
		public BundleViewModel BundleViewModel { get; set; }
		public RetentionViewModel RetentionViewModel { get; set; }

		public DumpViewModel(DumpMetainfo DumpInfo, BundleViewModel bundleViewModel, Similarities similarities, RetentionViewModel RetentionViewModel) {
			this.DumpInfo = DumpInfo;
			this.BundleViewModel = bundleViewModel;
			this.Similarities = similarities;
			this.RetentionViewModel = RetentionViewModel;
		}

		public DumpViewModel(DumpMetainfo DumpInfo, BundleViewModel bundleViewModel) {
			this.DumpInfo = DumpInfo;
			this.BundleViewModel = bundleViewModel;
			this.Similarities = new Similarities(new Dictionary<DumpIdentifier, double>());
		}
	}
}
