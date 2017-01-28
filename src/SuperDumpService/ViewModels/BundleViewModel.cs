using SuperDumpService.Models;

namespace SuperDumpService.ViewModels {
	public class BundleViewModel {
		public string Id { get; set; }
		public DumpBundle Bundle { get; set; }

		public BundleViewModel(string id) {
			this.Id = id;
		}

		public BundleViewModel(DumpBundle bundle) {
			this.Bundle = bundle;
			this.Id = this.Bundle.Id;
		}
	}
}
