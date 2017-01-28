using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections;
using System.Diagnostics;

namespace SuperDump {
	// taken under MIT license from https://github.com/Microsoft/dotnetsamples/tree/master/Microsoft.Diagnostics.Runtime/CLRMD
	public class ObjectSet {
		private struct Entry {
			public ulong High;
			public ulong Low;
			public int Index;
		}

		private BitArray[] _data;
		private Entry[] _entries;
		private int _shift;
		private bool _zero;

		public ObjectSet(ClrHeap heap) {
			_shift = IntPtr.Size == 4 ? 3 : 4;
			int count = heap.Segments.Count;

			_data = new BitArray[count];
			_entries = new Entry[count];
#if DEBUG
			ulong last = 0;
#endif

			for (int i = 0; i < count; ++i) {
				var seg = heap.Segments[i];
#if DEBUG
				Debug.Assert(last < seg.Start);
				last = seg.Start;
#endif

				_data[i] = new BitArray(GetBitOffset(seg.Length));
				_entries[i].Low = seg.Start;
				_entries[i].High = seg.End;
				_entries[i].Index = i;
			}
		}

		public void Add(ulong value) {
			if (value == 0) {
				_zero = true;
				return;
			}

			int index = GetIndex(value);
			if (index == -1)
				return;

			int offset = GetBitOffset(value - _entries[index].Low);

			_data[index].Set(offset, true);
		}

		public bool Contains(ulong value) {
			if (value == 0) {
				return _zero;
			}

			int index = GetIndex(value);
			if (index == -1)
				return false;

			int offset = GetBitOffset(value - _entries[index].Low);

			return _data[index][offset];
		}

		private int GetBitOffset(ulong offset) {
			Debug.Assert(offset < int.MaxValue);
			return GetBitOffset((int)offset);
		}

		private int GetBitOffset(int offset) {
			return offset >> _shift;
		}

		private int GetIndex(ulong value) {
			int low = 0;
			int high = _entries.Length - 1;

			while (low <= high) {
				int mid = (low + high) >> 1;
				if (value < _entries[mid].Low)
					high = mid - 1;
				else if (value > _entries[mid].High)
					low = mid + 1;
				else
					return mid;
			}

			// Outside of the heap.
			return -1;
		}
	}
}
