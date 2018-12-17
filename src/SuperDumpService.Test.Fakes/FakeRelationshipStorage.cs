using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SuperDumpService.Models;
using SuperDumpService.Services;

namespace SuperDumpService.Test.Fakes {
	public class FakeRelationshipStorage : IRelationshipStorage {
		private readonly int WRITE_RELATIONSHIPS_DELAY_MS = 1;

		public bool DelaysEnabled { get; set; }

		public FakeRelationshipStorage() {
		}

		public Task<IDictionary<DumpIdentifier, double>> ReadRelationships(DumpIdentifier dumpId) {
			return Task.FromResult<IDictionary<DumpIdentifier, double>>(new Dictionary<DumpIdentifier, double>());
		}

		public async Task StoreRelationships(DumpIdentifier dumpId, IDictionary<DumpIdentifier, double> relationships) {
			if (DelaysEnabled) await Task.Delay(WRITE_RELATIONSHIPS_DELAY_MS);
		}

		public void Wipe(DumpIdentifier dumpId) {
			throw new System.NotImplementedException();
		}
	}
}