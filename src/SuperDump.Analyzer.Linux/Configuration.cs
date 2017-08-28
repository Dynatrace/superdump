namespace SuperDump.Analyzer.Linux {
	/// <summary>
	/// This class represents the configuration of the Linux core dump analysis.
	/// In future versions, the configuration may be exported to an dynamically loaded external file.
	/// </summary>
	public static class Configuration {

		/// <summary>
		/// Libunwind wrapper shared library. The libunwind wrapper provides an easy-to-use interface to access libunwind functionality.
		/// </summary>
		public const string WRAPPER = "unwindwrapper.so";

		/// <summary>
		/// Path to store and read debug symbols.
		/// This path is a docker mount and must also be set in the docker run command (see appsettings.json/LinuxAnalysisCommand)
		/// </summary>
		public const string DEBUG_SYMBOL_PATH = "/debugsymbols";

		/// <summary>
		/// Dump summary file provides the path to the executable file.
		/// If the summary file is not present, other methods are used to compute the executable path.
		/// However, these other methods may fail to identify the path because the dump may not even contain the full executable path.
		/// </summary>
		public const string SUMMARY_TXT = "summary.txt";

		/// <summary>
		/// Pattern to retrieve debug symbols. SD will try to retrieve debug symbols from this URL by sending a GET request.
		/// Replacements:
		/// {hash}: MD5 hash of the binary file
		/// {file}: binary filename
		/// Set to null or empty to disable debug symbol retrieval.
		/// </summary>
		public const string DEBUG_SYMBOL_URL_PATTERN = "";
	}
}
