using System;
using System.Runtime.InteropServices;

namespace SuperDump {
	internal static class NativeStructs {
		#region Advapi32.dll

		[StructLayout(LayoutKind.Sequential)]
		public struct WAITCHAIN_NODE_INFO {
			public WCT_OBJECT_TYPE ObjectType;
			public WCT_OBJECT_STATUS ObjectStatus;
			public _WAITCHAIN_NODE_INFO_UNION Union;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct _WAITCHAIN_NODE_INFO_UNION {
			[FieldOffset(0)]
			public _WAITCHAIN_NODE_INFO_LOCK_OBJECT LockObject;
			[FieldOffset(0)]
			public _WAITCHAIN_NODE_INFO_THREAD_OBJECT ThreadObject;
		}

		public unsafe struct _WAITCHAIN_NODE_INFO_LOCK_OBJECT {
			/*The name of the object. Object names are only available for certain object, such as mutexes. If the object does not have a name, this member is an empty string.*/
			public fixed byte ObjectName[128 * 2];
			/*This member is reserved for future use.*/
			public UInt64 Timeout;
			/*This member is reserved for future use.*/
			public UInt32 Alertable;
		}

		public struct _WAITCHAIN_NODE_INFO_THREAD_OBJECT {
			/*The process identifier.*/
			public UInt32 ProcessId;
			/*The thread identifier. For COM and ALPC, this member can be 0.*/
			public UInt32 ThreadId;
			/*The wait time.*/
			public UInt32 WaitTime;
			/*The number of context switches.*/
			public UInt32 ContextSwitches;
		}

		#endregion

		#region NtDll

		public unsafe struct PUBLIC_OBJECT_TYPE_INFORMATION {
			public UNICODE_STRING TypeName;
			public fixed uint Reserved[22];
		}

		public struct OBJECT_NAME_INFORMATION {
			public UNICODE_STRING Name;
		}

		#endregion

		#region WinBase

		[StructLayout(LayoutKind.Sequential)]
		public struct CRITICAL_SECTION {
			public IntPtr DebugInfo;
			public int LockCount;
			public int RecursionCount;
			public IntPtr OwningThread;
			public IntPtr LockSemaphore;
			public UIntPtr SpinCount;
		}

		#endregion

		#region Other

		[StructLayout(LayoutKind.Sequential)]
		public struct UNICODE_STRING : IDisposable {
			public ushort Length;
			public ushort MaximumLength;
			private IntPtr buffer;

			public UNICODE_STRING(string s) {
				Length = (ushort)(s.Length * 2);
				MaximumLength = (ushort)(Length + 2);
				buffer = Marshal.StringToHGlobalUni(s);
			}

			public void Dispose() {
				Marshal.FreeHGlobal(buffer);
				buffer = IntPtr.Zero;
			}

			public override string ToString() {
				return Marshal.PtrToStringUni(buffer);
			}
		}

		#endregion
	}

	#region Advapi32.dll Enums

	public enum WCT_OBJECT_TYPE {
		WctCriticalSectionType = 1,
		WctSendMessageType = 2,
		WctMutexType = 3,
		WctAlpcType = 4,
		WctComType = 5,
		WctThreadWaitType = 6,
		WctProcessWaitType = 7,
		WctThreadType = 8,
		WctComActivationType = 9,
		WctUnknownType = 10,
		WctMaxType = 11,
	}

	public enum WCT_OBJECT_STATUS {
		WctStatusNoAccess = 1,    // ACCESS_DENIED for this object 
		WctStatusRunning = 2,     // Thread status 
		WctStatusBlocked = 3,     // Thread status 
		WctStatusPidOnly = 4,     // Thread status 
		WctStatusPidOnlyRpcss = 5,// Thread status 
		WctStatusOwned = 6,       // Dispatcher object status 
		WctStatusNotOwned = 7,    // Dispatcher object status 
		WctStatusAbandoned = 8,   // Dispatcher object status 
		WctStatusUnknown = 9,     // All objects 
		WctStatusError = 10,      // All objects 
		WctStatusMax = 11
	}

	public enum SYSTEM_ERROR_CODES {
		/// <summary>
		/// Overlapped I/O operation is in progress. (997 (0x3E5))
		/// </summary>
		ERROR_IO_PENDING = 997
	}

	public enum WCT_SESSION_OPEN_FLAGS {
		WCT_SYNC_OPEN_FLAG = 0,
		WCT_ASYNC_OPEN_FLAG = 1
	}

	#endregion
}
