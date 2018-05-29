using Newtonsoft.Json;
using SuperDumpService.Models;
using SuperDumpService.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SuperDump.Models;
using Microsoft.Extensions.Options;
using SuperDumpModels;

namespace SuperDumpService.Services {
	/// <summary>
	/// for writing and reading of relationship information
	/// this implementation uses simple filebased storage
	/// </summary>
	public class RelationshipStorageFilebased {
		private readonly PathHelper pathHelper;
		private readonly IOptions<SuperDumpSettings> settings;

		public RelationshipStorageFilebased(PathHelper pathHelper, IOptions<SuperDumpSettings> settings) {
			this.pathHelper = pathHelper;
			this.settings = settings;
		}

		public async Task StoreRelationships(DumpIdentifier dumpId, IDictionary<DumpIdentifier, double> relationships) {
			List<KeyValuePair<DumpIdentifier, double>> data = relationships.ToList(); // use a list, otherwise complex key (DumpIdentifier) is problematic
			await File.WriteAllTextAsync(pathHelper.GetRelationshipsPath(dumpId.BundleId, dumpId.DumpId), JsonConvert.SerializeObject(data));
		}

		public async Task<IDictionary<DumpIdentifier, double>> ReadRelationships(DumpIdentifier dumpId) {
			string text = await File.ReadAllTextAsync(pathHelper.GetRelationshipsPath(dumpId.BundleId, dumpId.DumpId));
			var data = JsonConvert.DeserializeObject<List<KeyValuePair<DumpIdentifier, double>>>(text);
			return data.ToDictionary(x => x.Key, y => y.Value);
		}

		public void Wipe(DumpIdentifier dumpId) {
			File.Delete(pathHelper.GetRelationshipsPath(dumpId.BundleId, dumpId.DumpId));
		}
	}
}
