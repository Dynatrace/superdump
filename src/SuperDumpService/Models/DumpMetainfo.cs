using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.Models {
	public class DumpMetainfo {
		public string BundleId { get; set; }
		public string DumpId { get; set; }
		public string DumpFileName { get; set; } // original filename. just informational.
		public DumpType DumpType { get; set; } = DumpType.WindowsDump; // default to windows, for compatibility to existing repos (which will only contain windows dumps)
		public DateTime Created { get; set; }
		public DateTime Finished { get; set; }
		public DumpStatus Status { get; set; }
		public string ErrorMessage { get; set; }
		public List<SDFileEntry> Files { get; set; } = new List<SDFileEntry>();
	}

	public enum DumpStatus {
		Created, Downloading, Analyzing, Finished, Failed
	}

	public class SDFileEntry {
		public SDFileType Type { get; set; }
		public string FileName { get; set; }
		public DateTime ExpirationDate { get; set; }
	}

	public enum DumpType {
		// *.dmp        -> windows user-space minidump or user-space fulldump
		WindowsDump,
		
		// *.core.gz    -> linux coredump file
		LinuxCoreDump
	}

	public enum SDFileType {
		[Description("Primary Dump")]
		PrimaryDump,

		[Description("Results")]
		WinDbg,
		[Description("Results")]
		SuperDumpData,
		[Description("Results")]
		CustomTextResult, // this has a special meaning: in case there is no result.json file, this text result is shown instead
		[Description("Results")]
		DebugDiagResult,

		[Description("Logs")]
		SuperDumpLogfile,

		[Description("Other files")]
		LinuxLibraries,
		[Description("Other files")]
		SiblingFile,
		[Description("Other files")]
		Other,
	}
}
