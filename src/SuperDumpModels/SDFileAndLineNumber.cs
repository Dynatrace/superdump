using Newtonsoft.Json;
using SuperDump.Models;
using System;

namespace SuperDumpModels {
	public class SDFileAndLineNumber : IEquatable<SDFileAndLineNumber>, ISerializableJson {
		public string File;
		public int Line;

		public string FileName() {
			int lastSlash = File.LastIndexOfAny(new char[] { '/', '\\' });
			if(lastSlash > 0) {
				return File.Substring(lastSlash + 1);
			}
			return File;
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
