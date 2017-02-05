using Microsoft.Extensions.Options;
using SuperDump;
using SuperDumpService.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.Services {
	public class SymStoreService {
		private SymStoreHelper symStoreHelper;

		public SymStoreService(IOptions<SuperDumpSettings> settings) {
			this.symStoreHelper = new SymStoreHelper(settings.Value.LocalSymbolCache, settings.Value.SymStoreExex64, settings.Value.SymStoreExex86);
		}

		internal async Task AddSymbols(string path) {
			if (!CanBePutInSymbolStore(path)) return;
			symStoreHelper.AddToSymStore(path);
		}

		private static bool CanBePutInSymbolStore(string path) {
			return path.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
		}
	}
}
