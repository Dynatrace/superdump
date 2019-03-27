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
	public class RelationshipStorageFilebased : IRelationshipStorage {
		private readonly PathHelper pathHelper;

		public RelationshipStorageFilebased(PathHelper pathHelper) {
			this.pathHelper = pathHelper;
		}

		public async Task StoreRelationships(DumpIdentifier id, IDictionary<DumpIdentifier, double> relationships) {
			List<KeyValuePair<DumpIdentifier, double>> data = relationships.OrderByDescending(x => Math.Round(x.Value, 3)).ToList(); // use a list, otherwise complex key (DumpIdentifier) is problematic
			await File.WriteAllTextAsync(pathHelper.GetRelationshipsPath(id), JsonConvert.SerializeObject(data, new DumpIdentifierConverter()));
		}

		public async Task<IDictionary<DumpIdentifier, double>> ReadRelationships(DumpIdentifier id) {
			string text = await File.ReadAllTextAsync(pathHelper.GetRelationshipsPath(id));
			var data = JsonConvert.DeserializeObject<List<KeyValuePair<DumpIdentifier, double>>>(text, new DumpIdentifierConverter());
			return data.ToDictionary(x => x.Key, y => y.Value);
		}

		public void Wipe(DumpIdentifier id) {
			File.Delete(pathHelper.GetRelationshipsPath(id));
		}
	}
}
