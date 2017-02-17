using SuperDumpService.Models;

namespace SuperDumpService.ViewModels {
	public class FileViewModel {
		public string BundleId { get; private set; }
		public string DumpId { get; private set; }
		public SDFileInfo File { get; private set; }

		public FileViewModel(string bundleId, string dumpId, SDFileInfo file) {
			this.BundleId = bundleId;
			this.DumpId = dumpId;
			this.File = file;
		}
	}
}
