using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SuperDumpService.Models;
using SuperDumpService.Services;

namespace SuperDumpService.Test.Fakes {
	public class FakeIdenticalDumpStorage : IIdenticalDumpStorage {
		public Task<HashSet<string>> Read(string bundleId) {
			throw new System.NotImplementedException();
		}

		public Task Store(string originalBundleId, string identicalBundleId) {
			throw new System.NotImplementedException();
		}

		public void Wipe(string bundleId) {
			throw new System.NotImplementedException();
		}
	}
}