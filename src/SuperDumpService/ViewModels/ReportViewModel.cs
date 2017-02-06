﻿using SuperDump.Models;
using SuperDumpService.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace SuperDumpService.ViewModels {
	public class ReportViewModel {
		public string BundleId { get; set; }
		public string DumpId { get; set; }
		public string BundleFileName { get; set; }
		public string DumpFileName { get; set; }
		public DateTime TimeStamp { get; set; }
		public SDResult Result { get; set; }
		public bool HasAnalysisFailed { get; set; }
		public string AnalysisError { get; set; }
		public IEnumerable<string> Files { get; set; }
		public ISet<SDTag> ThreadTags { get; set; }
		public int PointerSize { get; set; }
		public IDictionary<string, string> CustomProperties { get; set; } = new Dictionary<string, string>();

		public ReportViewModel(string bundleId, string dumpId) {
			this.BundleId = bundleId;
			this.DumpId = dumpId;
		}

		private static readonly string[] sizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
		public string SizeSuffix(ulong value) {
			if (value == 0) { return "0 bytes"; }

			int mag = (int)Math.Log(value, 1024);
			decimal adjustedSize = (decimal)value / (1L << (mag * 10));

			return string.Format("{0:n1}{1}", adjustedSize, sizeSuffixes[mag]).Replace(",", ".");
		}
	}
}
