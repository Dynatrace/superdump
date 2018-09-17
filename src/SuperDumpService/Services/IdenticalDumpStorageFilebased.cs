using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SuperDumpService.Helpers;

namespace SuperDumpService.Services {
	public class IdenticalDumpStorageFilebased {
		private readonly PathHelper pathHelper;
		private readonly IOptions<SuperDumpSettings> settings;

		public IdenticalDumpStorageFilebased(PathHelper pathHelper, IOptions<SuperDumpSettings> settings) {
			this.pathHelper = pathHelper;
			this.settings = settings;
		}

		public async Task Store(string originalBundleId, string identicalBundleId) {
			string path = pathHelper.GetIdenticRelationshipsPath(originalBundleId);

			HashSet<string> currentRelationships = File.Exists(path) ? await Read(originalBundleId) : new HashSet<string>();
			currentRelationships.Add(identicalBundleId);

			if (Directory.Exists(Path.GetDirectoryName(path)))
				await File.WriteAllTextAsync(path, JsonConvert.SerializeObject(currentRelationships));
		}

		public async Task<HashSet<string>> Read(string bundleId) {
			string path = pathHelper.GetIdenticRelationshipsPath(bundleId);
			if (!File.Exists(path)) {
				return null;
			}
			string text = await File.ReadAllTextAsync(path);
			return JsonConvert.DeserializeObject<HashSet<string>>(text);
		}

		public void Wipe(string bundleId) {
			File.Delete(pathHelper.GetIdenticRelationshipsPath(bundleId));
		}
	}
}
