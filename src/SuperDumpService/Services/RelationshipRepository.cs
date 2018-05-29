using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SuperDumpService.Models;

namespace SuperDumpService.Services {

	public class RelationshipRepository {
		private readonly object sync = new object();

		// stores relationships bi-directional. both directions should stay in sync
		private readonly IDictionary<DumpIdentifier, Dictionary<DumpIdentifier, Relationship>> relationShips = new Dictionary<DumpIdentifier, Dictionary<DumpIdentifier, Relationship>>();

		public void UpdateSimilarity(DumpIdentifier dumpA, DumpIdentifier dumpB, CrashSimilarity similarity) {
			lock (sync) {
				UpdateSimilarity0(dumpA, dumpB, similarity);
				UpdateSimilarity0(dumpB, dumpA, similarity);
			}
		}

		private void UpdateSimilarity0(DumpIdentifier dumpA, DumpIdentifier dumpB, CrashSimilarity similarity) {
			if (relationShips.TryGetValue(dumpA, out Dictionary<DumpIdentifier, Relationship> relationShip)) {
				UpdateRelationship(dumpA, dumpB, similarity, relationShip);
			} else {
				var dict = new Dictionary<DumpIdentifier, Relationship>();
				UpdateRelationship(dumpA, dumpB, similarity, dict);
				relationShips[dumpA] = dict;
			}
		}

		private static void UpdateRelationship(DumpIdentifier dumpA, DumpIdentifier dumpB, CrashSimilarity similarity, Dictionary<DumpIdentifier, Relationship> relationShip) {
			if (relationShip.TryGetValue(dumpB, out Relationship rel)) {
				rel.CrashSimilarity = similarity;
			} else {
				relationShip[dumpB] = new Relationship(dumpA, dumpB) { CrashSimilarity = similarity };
			}
		}

		public Relationship GetRelationShip(DumpIdentifier dumpA, DumpIdentifier dumpB) {
			lock (sync) {
				if (relationShips.TryGetValue(dumpA, out Dictionary<DumpIdentifier, Relationship> relationShips1)) {
					if (relationShips1.TryGetValue(dumpB, out Relationship rel1)) return rel1;
				}
				if (relationShips.TryGetValue(dumpB, out Dictionary<DumpIdentifier, Relationship> relationShips2)) {
					if (relationShips2.TryGetValue(dumpB, out Relationship rel2)) return rel2;
				}
				return null;
			}
		}

		public IEnumerable<Relationship> GetRelationShips(DumpIdentifier dumpA) {
			lock (sync) {
				if (relationShips.TryGetValue(dumpA, out Dictionary<DumpIdentifier, Relationship> relationShips1)) {
					return relationShips1.Values;
				}
				return Enumerable.Empty<Relationship>();
			}
		}
	}

}
