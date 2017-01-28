using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;

namespace SuperDump.Models {
	public class SDCombinedStackTrace : ICollection<SDCombinedStackFrame>, ISerializableJson {
		public IList<SDCombinedStackFrame> Trace { get; private set; } = new List<SDCombinedStackFrame>();

		public SDCombinedStackTrace(IList<SDCombinedStackFrame> trace) {
			this.Trace = trace;
		}

		public int Count {
			get { return Trace.Count; }
		}

		public bool IsReadOnly {
			get { return Trace.IsReadOnly; }
		}

		public void Add(SDCombinedStackFrame item) {
			Trace.Add(item);
		}

		public void Clear() {
			Trace.Clear();
		}

		public bool Contains(SDCombinedStackFrame item) {
			return Trace.Contains(item);
		}

		public void CopyTo(SDCombinedStackFrame[] array, int arrayIndex) {
			Trace.CopyTo(array, arrayIndex);
		}

		public IEnumerator<SDCombinedStackFrame> GetEnumerator() {
			return Trace.GetEnumerator();
		}

		public bool Remove(SDCombinedStackFrame item) {
			return Trace.Remove(item);
		}

		public string SerializeToJSON() {
			return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings {
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			});
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return Trace.GetEnumerator();
		}

		public SDCombinedStackFrame this[int i] {
			get { return Trace[i]; }
			set { Trace[i] = value; }
		}
	}
}
