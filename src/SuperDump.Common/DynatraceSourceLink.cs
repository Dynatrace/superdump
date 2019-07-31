using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SuperDump.Common {
	public class DynatraceSourceLink {
		private static readonly Regex SprintPathRegex = new Regex(@"^.*\\oa-s(\d*)\\[^\\]*\\(.*)$");

		public static string GetRepoPathIfAvailable(string sourcePath) {
			Match match;
			if (sourcePath.Contains("sprint_")) {
				int sprintOffset = sourcePath.IndexOf("sprint_");
				return "branches/" + sourcePath.Substring(sprintOffset);
			} else if ((match = SprintPathRegex.Match(sourcePath)).Success) {
				return $"branches/sprint_{match.Groups[1]}/{match.Groups[2]}";
			} else if (sourcePath.Contains("trunk")) {
				int trunkOffset = sourcePath.IndexOf("trunk");
				return sourcePath.Substring(trunkOffset);
			} else {
				return null;
			}
		}
	}
}
