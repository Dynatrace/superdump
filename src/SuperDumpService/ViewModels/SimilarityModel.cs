using SuperDumpService.Models;
using System;
using System.Collections.Generic;

namespace SuperDumpService.ViewModels {
	public class SimilarityModel {
		public DumpIdentifier DumpA;
		public DumpIdentifier DumpB;
		public CrashSimilarity CrashSimilarity;

		public string Error;

		public SimilarityModel(string error) {
			this.Error = error;
		}

		public SimilarityModel(DumpIdentifier dumpA, DumpIdentifier dumpB, CrashSimilarity crashSimilarity) {
			this.DumpA = dumpA;
			this.DumpB = dumpB;
			this.CrashSimilarity = crashSimilarity;
		}
	}
}
