namespace SuperDumpService {
	public class SuperDumpSettings {
		public string LocalSymbolCache { get; set; }
		public string SymStoreExex86 { get; set; }
		public string SymStoreExex64 { get; set; }
		public int MaxConcurrentAnalysis { get; set; }
		public int MaxConcurrentBundleExtraction { get; set; }
	}
}
