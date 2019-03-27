using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SuperDump.Models;

namespace SuperDumpService.Models {
	/// <summary>
	/// This class aims to provide data for similarity detection, but aims to be as small as possible (memory footprint)
	/// </summary>
	/// 
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct DumpMiniInfo {
		/// <summary>
		/// version 3: Hashes instead of strings
		/// </summary>
		public int DumpSimilarityInfoVersion { get; set; }

		public ThreadMiniInfo? FaultingThread { get; set; }
		public LastEventMiniInfo? LastEvent { get; set; }
		public ExceptionMiniInfo? Exception { get; set; }
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct ThreadMiniInfo {
		public int[] DistinctModuleHashes { get; set; }
		public int[] DistinctFrameHashes { get; set; }
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct ExceptionMiniInfo {
		public int? TypeHash { get; set; }
		public int? MessageHash { get; set; }
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct LastEventMiniInfo {
		public int? TypeHash { get; set; }
		public int? DescriptionHash { get; set; }
	}
}
