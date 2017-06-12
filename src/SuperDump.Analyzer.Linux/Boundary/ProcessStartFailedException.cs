using System;
using System.Collections.Generic;
using System.Text;

namespace SuperDump.Analyzer.Linux.Boundary {
	public class ProcessStartFailedException : Exception {
		public ProcessStartFailedException(Exception e) 
			: base("Failed to start process!", e) {
		}
	}
}
