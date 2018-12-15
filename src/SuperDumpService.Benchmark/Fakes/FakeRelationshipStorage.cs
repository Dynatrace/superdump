using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SuperDumpService.Models;
using SuperDumpService.Services;

namespace SuperDumpService.Benchmarks.Fakes {
	internal class FakeRelationshipStorage : IRelationshipStorage {
		private readonly int WRITE_RELATIONSHIPS_DELAY_MS = 1;

		public FakeRelationshipStorage() {
		}

		public Task<IDictionary<DumpIdentifier, double>> ReadRelationships(DumpIdentifier dumpId) {
			return Task.FromResult<IDictionary<DumpIdentifier, double>>(new Dictionary<DumpIdentifier, double>());
		}

		public Task StoreRelationships(DumpIdentifier dumpId, IDictionary<DumpIdentifier, double> relationships) {
			Thread.Sleep(WRITE_RELATIONSHIPS_DELAY_MS);
			return Task.Delay(0);
		}

		public void Wipe(DumpIdentifier dumpId) {
			throw new System.NotImplementedException();
		}
	}
}