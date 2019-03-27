using System.Collections.Generic;
using System.Threading.Tasks;
using SuperDumpService.Models;

namespace SuperDumpService.Services {
	public interface IRelationshipStorage {
		Task<IDictionary<DumpIdentifier, double>> ReadRelationships(DumpIdentifier dumpId);
		Task StoreRelationships(DumpIdentifier dumpId, IDictionary<DumpIdentifier, double> relationships);
		void Wipe(DumpIdentifier dumpId);
	}
}