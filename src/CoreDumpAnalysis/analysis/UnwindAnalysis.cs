using SuperDump.Models;
using SuperDumpModels;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CoreDumpAnalysis {
	public class UnwindAnalysis {
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

		private readonly IFilesystem filesystem;
		private readonly SDResult analysisResult;
		private readonly String coredump;

		public UnwindAnalysis(IFilesystem filesystemHelper, String coredump, SDResult result) {
			this.filesystem = filesystemHelper ?? throw new ArgumentNullException("FilesystemHelper must not be null!");
			this.analysisResult = result ?? throw new ArgumentNullException("SD Result must not be null!");
			this.coredump = coredump ?? throw new ArgumentNullException("Coredump Path must not be null!");
		}

		public void DebugAndSetResultFields() {
			String parent = filesystem.GetParentDirectory(coredump);
			parent = parent.Substring(0, parent.Length - 1);
			init(this.coredump, parent);

			SDCDSystemContext context = analysisResult.SystemContext as SDCDSystemContext ?? new SDCDSystemContext();
			SetContextFields(context);
			this.analysisResult.SystemContext = context;
			this.analysisResult.ThreadInformation = UnwindThreads(context);
		}

		private SDCDSystemContext SetContextFields(SDCDSystemContext context) {
			context.ProcessArchitecture = "N/A";
			context.SystemUpTime = "Could not be obtained.";
			context.Modules = new List<SDModule>();
			context.AppDomains = new List<SDAppDomain>();
			context.ClrVersions = new List<SDClrVersion>();
			SetAuxvFields(context);
			SharedLibAdapter sharedLibAdapter = new SharedLibAdapter(filesystem);
			new SharedLibExtractor().ExtractSharedLibs().ForEach(lib => {
				SDCDModule sharedLib = sharedLibAdapter.Adapt(lib);
				if (sharedLib != null) {
					context.Modules.Add(sharedLib);
				}
			});
			return context;
		}

		private Dictionary<uint, SDThread> UnwindThreads(SDCDSystemContext context) {
			Dictionary<uint, SDThread> threads = new Dictionary<uint, SDThread>();

			int nThreads = getNumberOfThreads();
			for (uint i = 0; i < nThreads; i++) {
				selectThread(i);
				SDThread thread = new SDThread() {
					EngineId = i,
					OsId = i,
					Index = i
				};
				UnwindCurrentThread(context, thread);
				threads.Add(i, thread);
			}
			return threads;
		}

		private void UnwindCurrentThread(SDCDSystemContext context, SDThread thread) {
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
					frames.Add(new SDCombinedStackFrame(StackFrameType.Native, "", oldProcName, oldOffset, oldIp, oldSp, ip, 0, null));
				}
				oldIp = ip;
				oldSp = sp;
				oldOffset = offset;
				oldProcName = procName;
			} while (!step() && ++nFrames < MAX_FRAMES);

			thread.StackTrace = new SDCombinedStackTrace(frames);
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
