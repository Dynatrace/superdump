using System.Runtime.InteropServices;

namespace SuperDump {
	public static class Utility {
		public static void CheckHRESULT(int hr) {
			if (hr != 0) {
				Marshal.ThrowExceptionForHR(hr); //interop class for working with unmanaged code
			}
		}
	}
}
