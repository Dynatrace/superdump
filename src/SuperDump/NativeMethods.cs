using System;
using System.Runtime.InteropServices;
using FileTime = System.Runtime.InteropServices.ComTypes.FILETIME;
using static SuperDump.NativeStructs;

namespace SuperDump {
	internal static class NativeMethods {
		#region Advapi32.dll

		// WCT API

		[DllImport("Advapi32.dll", SetLastError = true)]
		internal static extern void CloseThreadWaitChainSession(IntPtr WctIntPtr);

		[DllImport("Advapi32.dll", SetLastError = true)]
		internal static extern IntPtr OpenThreadWaitChainSession(UInt32 Flags, UInt32 callback);

		[DllImport("Advapi32.dll", SetLastError = true)]
		internal static extern bool GetThreadWaitChain(
			IntPtr WctIntPtr,
			IntPtr Context,
			UInt32 Flags,
			uint ThreadId,
			ref int NodeCount,
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
			[In, Out]
			WAITCHAIN_NODE_INFO[] NodeInfoArray,
			out int IsCycle
		);

		[DllImport("kernel32.dll")]
		internal static extern bool GetProcessTimes(IntPtr hProcess, out FileTime lpCreationTime,
			out FileTime lpExitTime, out FileTime lpKernelTime, out FileTime lpUserTime);
		#endregion
	}
}
