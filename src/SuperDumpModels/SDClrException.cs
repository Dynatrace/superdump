using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SuperDump.Models {
	public class SDClrException : IEquatable<SDClrException>, ISerializableJson {
		public ulong OSThreadId { get; set; }
		public string Type { get; set; }
		public ulong Address { get; set; }
		public int HResult { get; set; }
		public SDClrException InnerException { get; set; }
		public string Message { get; set; } = "";
		public SDCombinedStackTrace StackTrace { get; set; } = new SDCombinedStackTrace(new List<SDCombinedStackFrame>());

		public SDClrException() { }

		public override int GetHashCode() {
			return base.GetHashCode();
		}

		public override bool Equals(object obj) {
			if (obj is SDClrException) {
				var exception = obj as SDClrException;
				return this.Equals(exception);
			}
			return false;
		}

		public bool Equals(SDClrException other) {
			bool equals = false;
			if (this.Address.Equals(other.Address)
				&& this.HResult.Equals(other.HResult)
				&& this.InnerException.Equals(InnerException)
				&& this.Message.Equals(other.Message)
				&& this.StackTrace.SequenceEqual(other.StackTrace)) {
				equals = true;
			}
			return equals;
		}

		public string SerializeToJSON() {
			return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings {
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			});
		}
	}
}
