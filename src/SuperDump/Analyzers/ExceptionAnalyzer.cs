using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;
using SuperDump.Models;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SuperDump.Analyzers {
	public class ExceptionAnalyzer {
		public SDResult res;
		private IDebugClient debugClient;
		private DumpContext context;

		public ExceptionAnalyzer(DumpContext context, SDResult res) {
			this.context = context;
			this.res = res;
			using (DataTarget t = this.context.CreateTemporaryDbgEngTarget()) {
				this.debugClient = t.DebuggerInterface;
				this.InitLastEvent();
			}
		}

		private void InitLastEvent() {
			this.res.LastEvent = GetLastEventInformation();
		}

		private SDLastEvent GetLastEventInformation() {
			try {
				uint extraInformationSize = 500;
				var buffer = new byte[extraInformationSize];
				GCHandle pinnedArray = GCHandle.Alloc(buffer, GCHandleType.Pinned);
				try {
					DEBUG_EVENT type;
					uint processId;
					uint threadId;
					IntPtr extraInformation = pinnedArray.AddrOfPinnedObject();
					uint extraInformationUsed;
					int descriptionSize = 500;
					var description = new StringBuilder(descriptionSize);
					uint descriptionUsed;
					Utility.CheckHRESULT(DebugControl.GetLastEventInformation(
						out type, out processId, out threadId, extraInformation, extraInformationSize,
						out extraInformationUsed, description, descriptionSize, out descriptionUsed));

					return new SDLastEvent() {
						Type = type.ToString(),
						Description = description.ToString(),
						ThreadId = threadId
					};
				} finally {
					pinnedArray.Free();
				}
			} catch (Exception) {
				return null;
			}
		}

		private IDebugControl DebugControl {
			get { return (IDebugControl6)this.debugClient; }
		}

		private IDebugClient6 DebugClient {
			get { return (IDebugClient6)this.debugClient; }
		}
	}
}
