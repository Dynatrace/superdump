using SuperDumpService.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SuperDumpService.ViewModels {
	public class Similarities {
		public IEnumerable<KeyValuePair<DumpIdentifier, double>> Values { get; set; } = Enumerable.Empty<KeyValuePair<DumpIdentifier, double>>();

		public Similarities(IDictionary<DumpIdentifier, double> similarities) {
			this.Values = similarities;
		}

		public Similarities(IEnumerable<KeyValuePair<DumpIdentifier, double>> similarities) {
			this.Values = similarities;
		}

		public IEnumerable<KeyValuePair<DumpIdentifier, double>> AboveThresholdSimilarities() {
			return Values.Where(x => x.Value > 0.8);
		}
	}
}
