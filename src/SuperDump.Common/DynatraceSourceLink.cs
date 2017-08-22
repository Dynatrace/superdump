using System;
using System.Collections.Generic;
using System.Text;

namespace SuperDump.Common {
	public class DynatraceSourceLink {
		public static string GetRepoPathIfAvailable(string sourcePath) {
			if (sourcePath.Contains("sprint_")) {
				int sprintOffset = sourcePath.IndexOf("sprint_");
				return "branches/" + sourcePath.Substring(sprintOffset);
			} else if (sourcePath.Contains("trunk")) {
				int trunkOffset = sourcePath.IndexOf("trunk");
				return sourcePath.Substring(trunkOffset);
			} else {
				return null;
			}
		}
	}
}
