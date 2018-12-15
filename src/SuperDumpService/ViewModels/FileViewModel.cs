using SuperDumpService.Models;

namespace SuperDumpService.ViewModels {
	public class FileViewModel {
		public DumpIdentifier Id { get; private set; }
		public SDFileInfo File { get; private set; }

		public FileViewModel(DumpIdentifier id, SDFileInfo file) {
			this.Id = id;
			this.File = file;
		}
	}
}
