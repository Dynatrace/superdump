using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.Models
{
    public class SDFileInfo { 
		public FileInfo	FileInfo { get; set; }

		public SDFileEntry FileEntry { get; set; }
		public long SizeInBytes { get; set; }
		public bool Downloadable { get; set; }
	}
}
