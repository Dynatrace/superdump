using System.Collections.Generic;
using System.Threading.Tasks;

namespace SuperDumpService.Services {
	public interface IIdenticalDumpStorage {
		Task<HashSet<string>> Read(string bundleId);
		Task Store(string originalBundleId, string identicalBundleId);
		void Wipe(string bundleId);
	}
}