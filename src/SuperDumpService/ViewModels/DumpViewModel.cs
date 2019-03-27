using SuperDumpService.Models;
using System;
using System.Collections.Generic;

namespace SuperDumpService.ViewModels {
	public class DumpViewModel {
		public DumpMetainfo DumpInfo { get; set; }
		public Similarities Similarities { get; set; }
		public BundleViewModel BundleViewModel { get; set; }

		public DumpViewModel(DumpMetainfo DumpInfo, BundleViewModel bundleViewModel, Similarities similarities) {
			this.DumpInfo = DumpInfo;
			this.BundleViewModel = bundleViewModel;
			this.Similarities = similarities;
		}

		public DumpViewModel(DumpMetainfo DumpInfo, BundleViewModel bundleViewModel) {
			this.DumpInfo = DumpInfo;
			this.BundleViewModel = bundleViewModel;
			this.Similarities = new Similarities(new Dictionary<DumpIdentifier, double>());
		}
	}
}
