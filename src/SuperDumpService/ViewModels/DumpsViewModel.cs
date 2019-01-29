using Sakura.AspNetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.ViewModels {
	public class DumpsViewModel {
		public IEnumerable<DumpViewModel> All { get; set; }
		public IEnumerable<DumpViewModel> Filtered { get; set; }
		public IPagedList<DumpViewModel> Paged { get; set; }
		public bool IsPopulated { get; set; }
		public bool IsRelationshipsPopulated { get; set; }
		public bool IsJiraIssuesPopulated { get; set; }
		public string KibanaUrl { get; set; }
	}
}