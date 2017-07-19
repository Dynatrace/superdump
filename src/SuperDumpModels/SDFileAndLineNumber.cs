using Newtonsoft.Json;
using SuperDump.Models;
using System;
using System.IO;

namespace SuperDumpModels {
	public class SDFileAndLineNumber : IEquatable<SDFileAndLineNumber>, ISerializableJson {
		public string File;
		public int Line;

		public string FileName() {
			return File;
		}

		public bool IsLinkAvailable() {
			return File.Contains("/agent/native") && File.Contains("sprint_");
		}

		public string GetLink() {
			if (File.Contains("sprint_")) {
				int sprintOffset = File.IndexOf("sprint_");
				return "branches/" + File.Substring(sprintOffset);
			} else if (File.Contains("trunk/")) {
				int trunkOffset = File.IndexOf("trunk/");
				return File.Substring(trunkOffset);
			} else {
				return null;
			}
		}

		public string SerializeToJSON() {
			return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings {
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			});
		}

		public override bool Equals(object obj) {
			if (obj is SDFileAndLineNumber) {
				var context = obj as SDFileAndLineNumber;
				return this.Equals(context);
			} else {
				return false;
			}
		}

		public override int GetHashCode() {
			return base.GetHashCode();
		}

		public bool Equals(SDFileAndLineNumber other) {
			return this.File == other.File
				&& this.Line == other.Line;
		}
	}
}
