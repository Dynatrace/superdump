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
