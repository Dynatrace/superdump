using SuperDump.Models;
using SuperDumpModels;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace CoreDumpAnalysis {
	class CoreDumpAnalysis {
		public const int MAX_FRAMES = 128;

		[DllImport(Constants.WRAPPER)]
		private static extern void init(string filepath, string workindDir);

		[DllImport(Constants.WRAPPER)]
		private static extern int getNumberOfThreads();

		[DllImport(Constants.WRAPPER)]
		private static extern int getThreadId();

		[DllImport(Constants.WRAPPER)]
		private static extern void selectThread(uint threadNumber);

		[DllImport(Constants.WRAPPER)]
		private static extern ulong getInstructionPointer();

		[DllImport(Constants.WRAPPER)]
		private static extern ulong getStackPointer();

		[DllImport(Constants.WRAPPER)]
		private static extern string getProcedureName();

		[DllImport(Constants.WRAPPER)]
		private static extern ulong getProcedureOffset();

		[DllImport(Constants.WRAPPER)]
		private static extern bool step();

		[DllImport(Constants.WRAPPER)]
		private static extern ulong getAuxvValue(int type);

		[DllImport(Constants.WRAPPER)]
		private static extern string getAuxvString(int type);

		public SDResult Debug(String coredump) {
			String parent = FilesystemHelper.GetParentDirectory(coredump);
			parent = parent.Substring(0, parent.Length - 1);
			init(coredump, parent);

			SDCDSystemContext context = BuildContext();
			List<string> notLoadedSymbols = new List<string>();
			List<SDDeadlockContext> deadlocks = new List<SDDeadlockContext>();
			Dictionary<ulong, SDMemoryObject> memoryObjects = new Dictionary<ulong, SDMemoryObject>();
			Dictionary<uint, SDThread> threads = UnwindThreads(context);
			List<SDClrException> exceptions = new List<SDClrException>();
			SDLastEvent lastEvent = null;

			return new SDResult(context, lastEvent, exceptions, threads, memoryObjects, deadlocks, notLoadedSymbols);
		}

		private SDCDSystemContext BuildContext() {
			SDCDSystemContext context = new SDCDSystemContext();
			context.ProcessArchitecture = "N/A";
			context.SystemUpTime = "Could not be obtained.";
			context.Modules = new List<SDModule>();
			context.AppDomains = new List<SDAppDomain>();
			context.ClrVersions = new List<SDClrVersion>();
			SetAuxvFields(context);
			SharedLibAdapter sharedLibAdapter = new SharedLibAdapter();
			new SharedLibExtractor().ExtractSharedLibs().ForEach(lib => {
				context.Modules.Add(sharedLibAdapter.Adapt(lib));
			});
			return context;
		}

		private Dictionary<uint, SDThread> UnwindThreads(SDCDSystemContext context) {
			Dictionary<uint, SDThread> threads = new Dictionary<uint, SDThread>();

			int nThreads = getNumberOfThreads();
			Console.WriteLine("Threads: " + nThreads);
			Console.WriteLine("Instruction Pointer\tStack Pointer\t\tProcedure Name + Offset");
			for (uint i = 0; i < nThreads; i++) {
				selectThread(i);
				Console.WriteLine();
				Console.WriteLine("Thread: " + i);
				List<SDCombinedStackFrame> frames = new List<SDCombinedStackFrame>();

				ulong ip, oldIp = 0, sp, oldSp = 0, offset, oldOffset = 0;
				String procName, oldProcName = null;
				int nFrames = 0;
				do {
					ip = getInstructionPointer();
					sp = getStackPointer();
					procName = getProcedureName();
					offset = getProcedureOffset();

					if (oldProcName != null) {
						Console.WriteLine("{0:X16}\t{1:X16}\t{2}+{3}", getInstructionPointer(), getStackPointer(), getProcedureName(), getProcedureOffset());

						String curModuleName = "";
						foreach (SDModule module in context.Modules) {
							if (module.GetType() != typeof(SDCDModule)) {
								throw new InvalidCastException("Plain SDModule found in module list. SDCDModule expected.");
							}
							SDCDModule cdModule = (SDCDModule)module;
							if (cdModule.StartAddress < oldIp && cdModule.EndAddress > oldIp) {
								curModuleName = cdModule.FileName;
								break;
							}
						}
						frames.Add(new SDCombinedStackFrame(StackFrameType.Native, curModuleName, oldProcName, oldOffset, oldIp, oldSp, ip, 0, null));
					}
					oldIp = ip;
					oldSp = sp;
					oldOffset = offset;
					oldProcName = procName;
				} while (!step() && ++nFrames < MAX_FRAMES);

				SDThread thread = new SDThread(i);
				thread.EngineId = i;
				thread.OsId = i;
				thread.Index = i;
				thread.StackTrace = new SDCombinedStackTrace(frames);
				threads.Add(i, thread);
			}
			return threads;
		}

		private void SetAuxvFields(SDCDSystemContext context) {
			context.PageSize = (int)getAuxvValue(AuxType.PAGE_SIZE.Type);
			context.EntryPoint = getAuxvValue(AuxType.ENTRY_POINT.Type);
			context.BasePlatform = getAuxvString(AuxType.BASE_PLATFORM.Type);
			context.Uid = (int)getAuxvValue(AuxType.UID.Type);
			context.Euid = (int)getAuxvValue(AuxType.EUID.Type);
			context.Gid = (int)getAuxvValue(AuxType.GID.Type);
			context.Egid = (int)getAuxvValue(AuxType.EGID.Type);
			context.SystemArchitecture = getAuxvString(AuxType.PLATFORM.Type);
		}
	}
}
