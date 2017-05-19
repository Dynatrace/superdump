using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CoreDumpAnalysis {
	public class SharedLibExtractor {
		[DllImport(Constants.WRAPPER, CallingConvention = CallingConvention.Cdecl)]
		[return: MarshalAs(UnmanagedType.I1)]
		private static extern bool getSharedLibs(out int size, out IntPtr libs);

		public List<SharedLib> ExtractSharedLibs() {
			var arrayValue = IntPtr.Zero;
			var size = 0;
			var list = new List<SharedLib>();

			if (!getSharedLibs(out size, out arrayValue)) {
				return list;
			}

			Console.WriteLine("Detected " + size + " shared libraries.");

			var tableEntrySize = Marshal.SizeOf(typeof(SharedLib));
			for (var i = 0; i < size; i++) {
				var cur = (SharedLib)Marshal.PtrToStructure(arrayValue, typeof(SharedLib));
				list.Add(cur);
				arrayValue = new IntPtr(arrayValue.ToInt32() + tableEntrySize);
			}
			return list;
		}
	}
}
