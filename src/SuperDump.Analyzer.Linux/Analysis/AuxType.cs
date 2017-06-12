using System;
using System.Collections.Generic;
using System.Text;

namespace SuperDump.Analyzer.Linux.Analysis {
	public class AuxType {
		public static readonly AuxType PAGE_SIZE = new AuxType(6);
		public static readonly AuxType FLAGS = new AuxType(8);
		public static readonly AuxType ENTRY_POINT = new AuxType(9);
		public static readonly AuxType UID = new AuxType(11);
		public static readonly AuxType EUID = new AuxType(12);
		public static readonly AuxType GID = new AuxType(13);
		public static readonly AuxType EGID = new AuxType(14);
		public static readonly AuxType PLATFORM = new AuxType(15);

		public static readonly AuxType HWCAP = new AuxType(16);
		public static readonly AuxType BASE_PLATFORM = new AuxType(24);
		public static readonly AuxType HWCAP2 = new AuxType(26);
		public static readonly AuxType EXECFN = new AuxType(31);

		private int id;

		private AuxType(int id) {
			this.id = id;
		}

		public int Type {
			get { return id; }
		}
	}
}
