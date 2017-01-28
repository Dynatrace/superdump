using Microsoft.Diagnostics.Runtime;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperDump.Models {
	[Serializable]
	public class SDClrException : IEquatable<SDClrException>, ISerializableJson {
		public ulong Address { get; set; }
		public int HResult { get; set; }
		public SDClrException InnerException { get; set; }
		public string Message { get; set; } = "";
		public IList<CombinedStackFrame> StackTrace { get; set; }
		public SDClrException() {

		}

		public SDClrException(ClrException clrException) {
			if(clrException != null) {
				this.Address = clrException.Address;
				//this.HResult = clrException.HResult;

				if(this.InnerException != null)
					this.InnerException = new SDClrException(clrException.Inner);

				this.Message = clrException.GetExceptionMessageSafe();

				this.StackTrace = new List<CombinedStackFrame>();
				foreach(ClrStackFrame clrFrame in clrException.StackTrace) {
					this.StackTrace.Add(new CombinedStackFrame(clrFrame));
				}
			}
		}
		public override int GetHashCode() {
			return base.GetHashCode();
		}
		public override bool Equals(object obj) {
			if(obj is SDClrException) {
				var exception = obj as SDClrException;
				return this.Equals(exception);
			}
			return false;
		}
		public bool Equals(SDClrException other) {
			bool equals = false;
			if(this.Address.Equals(other.Address)
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
