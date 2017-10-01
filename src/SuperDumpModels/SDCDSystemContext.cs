using SuperDump.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDump.Models {
	public class SDCDSystemContext : SDSystemContext {
		public int Uid { get; set; } = -1;
		public int Euid { get; set; } = -1;
		public int Gid { get; set; } = -1;
		public int Egid { get; set; } = -1;

		public int PageSize { get; set; } = -1;
		public ulong EntryPoint { get; set; } = 0;

		public string BasePlatform { get; set; } = "";

		public string FileName { get; set; } = "";
		public string Args { get; set; } = "";
	}
}
