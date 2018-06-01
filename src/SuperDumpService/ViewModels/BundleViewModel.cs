using SuperDumpService.Models;
using System;
using System.Collections.Generic;

namespace SuperDumpService.ViewModels {
	public class BundleViewModel {
		public string BundleId { get; set; }
		public DateTime Created { get; set; }
		public BundleStatus Status { get; set; }
		public IDictionary<string, string> CustomProperties { get; set; }
		public IEnumerable<DumpListViewModel> DumpInfos { get; set; }
		public string ErrorMessage { get; set; }
		public string OriginalBundleId { get; set; }

		public BundleViewModel(BundleMetainfo bundleInfo, IEnumerable<DumpListViewModel> dumpInfos) {
			this.BundleId = bundleInfo.BundleId;
			this.Created = bundleInfo.Created;
			this.Status = bundleInfo.Status;
			this.CustomProperties = bundleInfo.CustomProperties;
			this.DumpInfos = dumpInfos ?? new List<DumpListViewModel>();
			this.ErrorMessage = bundleInfo.ErrorMessage;
			this.OriginalBundleId = bundleInfo.OriginalBundleId;
		}
	}
}
