using Sakura.AspNetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.ViewModels {
	public class OverviewViewModel {
		public IEnumerable<BundleViewModel> All { get; set; }
		public IEnumerable<BundleViewModel> Filtered { get; set; }
		public IPagedList<BundleViewModel> Paged { get; set; }
	}
}
