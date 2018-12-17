using System.Collections.Generic;
using System.Threading.Tasks;
using SuperDumpService.Models;

namespace SuperDumpService.Services {
	public interface IBundleStorage {
		Task<IEnumerable<BundleMetainfo>> ReadBundleMetainfos();
		void Store(BundleMetainfo bundleInfo);
	}
}