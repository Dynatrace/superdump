using Microsoft.Extensions.Options;
using SuperDump;
using SuperDumpService.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.Services {
	public class SymStoreService {
		private SymStoreHelper symStoreHelper;

		public SymStoreService(IOptions<SuperDumpSettings> settings) {
			this.symStoreHelper = new SymStoreHelper(settings.Value.LocalSymbolCache, settings.Value.SymStoreExex64, settings.Value.SymStoreExex86);
		}

		internal void AddSymbols(FileInfo file) {
			if (!CanBePutInSymbolStore(file)) return;
			symStoreHelper.AddToSymStore(file);
		}

		private static bool CanBePutInSymbolStore(FileInfo file) {
			return file.Extension.Equals(".pdb", StringComparison.OrdinalIgnoreCase) || file.Extension.Equals(".dll", StringComparison.OrdinalIgnoreCase);
		}
	}
}
