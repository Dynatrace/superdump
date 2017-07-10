using SuperDump.Analyzer.Linux.Boundary;
using SuperDump.Models;
using SuperDumpModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Thinktecture.IO;

namespace SuperDump.Analyzer.Linux.Analysis {
	public class UnwindAnalyzer {
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

		[DllImport(Constants.WRAPPER)]
		private static extern int getSignalNumber(int threadNo);

		[DllImport(Constants.WRAPPER)]
		private static extern int getSignalErrorNo(int threadNo);

		[DllImport(Constants.WRAPPER)]
		private static extern ulong getSignalAddress(int threadNo);

		[DllImport(Constants.WRAPPER)]
		private static extern string getFileName();

		[DllImport(Constants.WRAPPER)]
		private static extern string getArgs();
		[DllImport(Constants.WRAPPER)]
		private static extern int is64Bit();

		[DllImport(Constants.WRAPPER)]
		private static extern void destroy();

		private readonly SDResult analysisResult;
		private readonly IFileInfo coredump;

		private bool isDestroyed = false;

		public UnwindAnalyzer(IFileInfo coredump, SDResult result) {
			this.analysisResult = result ?? throw new ArgumentNullException("SD Result must not be null!");
			this.coredump = coredump ?? throw new ArgumentNullException("Coredump Path must not be null!");
		}

		public void Analyze() {
			if(isDestroyed) {
				throw new InvalidOperationException("Cannot use analysis on the same object twice!");
			}
			init(coredump.FullName, coredump.Directory.FullName);

			SDCDSystemContext context = analysisResult.SystemContext as SDCDSystemContext ?? new SDCDSystemContext();
			SetContextFields(context);
			this.analysisResult.SystemContext = context;
			this.analysisResult.ThreadInformation = UnwindThreads(context);
			Console.WriteLine("Destroying unwindwrapper context ...");
			destroy();
			isDestroyed = true;
		}

		private SDCDSystemContext SetContextFields(SDCDSystemContext context) {
			context.ProcessArchitecture = "N/A";
			context.SystemUpTime = "Could not be obtained.";
			context.AppDomains = new List<SDAppDomain>();
			context.ClrVersions = new List<SDClrVersion>();
			Console.WriteLine("Retrieving filename and args ...");
			context.FileName = getFileName();
			context.Args = getArgs();
			SetAuxvFields(context);
			return context;
		}

		private Dictionary<uint, SDThread> UnwindThreads(SDCDSystemContext context) {
			var threads = new Dictionary<uint, SDThread>();

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

			bool foundLastExecuted = false;
			for(int i = 0; i < nThreads; i++) {
				int signal = getSignalNumber(i);
				if(signal == -1) {
					continue;
				}
				if(signal < 32 && signal != 19) {
					if(foundLastExecuted) {
						Console.WriteLine("Already found the last executed thread which was: " + analysisResult.LastExecutedThread + ". New one is " + i);
					}
					foundLastExecuted = true;
					analysisResult.LastExecutedThread = (uint)i;
					analysisResult.LastEvent = new SDLastEvent() {
						ThreadId = (uint)i,
						Type = signal.ToString(),
						Description = SignalNoToCode(signal)
					};
					if (signal == 4 || signal == 8) {
						analysisResult.LastEvent.Description += ": Faulty instruction at address " + getSignalAddress(i);
					} else if(signal == 11) {
						analysisResult.LastEvent.Description += ": Invalid memory reference to address 0x" + getSignalAddress(i).ToString("X");
					} else {
						int error = getSignalErrorNo(i);
						if(error != 0) {
							analysisResult.LastEvent.Description += " (error number " + error + ")";
						}
					}
				}
			}
			return threads;
		}

		private string SignalNoToCode(int signal) {
			switch(signal) {
				case 1: return "SIGHUP";
				case 2: return "SIGINT";
				case 3: return "SIGQUIT";
				case 4: return "SIGILL";
				case 6: return "SIGABRT";
				case 7: return "SIGBUS";
				case 8: return "SIGFPE";
				case 9: return "SIGKILL";
				case 11: return "SIGSEGV";
				case 13: return "SIGPIPE";
				case 14: return "SIGALRM";
				case 15: return "SIGTERM";
				case 10: return "SIGUSR1";
				case 12: return "SIGUSR2";
				case 17: return "SIGCHLD";
				case 18: return "SIGCONT";
				case 19: return "SIGSTOP";
				case 20: return "SIGTSTP";
				case 21: return "SIGTTIN";
				case 22: return "SIGTTOU";
				case 27: return "SIGPROF";
				case 31: return "SIGSYS";
				case 5: return "SIGTRAP";
				case 23: return "SIGURG";
				case 26: return "SIGVTALRM";
				case 24: return "SIGXCPU";
				case 25: return "SIGXFSZ";
				case 16: return "SIGSTKFLT";
				case 29: return "SIGIO";
				case 30: return "SIGPWR";
				case 28: return "SIGWINCH";
				default: return "Unknown signal";
			}
		}

		private void UnwindCurrentThread(SDCDSystemContext context, SDThread thread) {
			var frames = new List<SDCombinedStackFrame>();

			ulong ip, oldIp = 0, sp, oldSp = 0, offset, oldOffset = 0;
			string procName, oldProcName = null;
			int nFrames = 0;
			do {
				ip = getInstructionPointer();
				sp = getStackPointer();
				procName = getProcedureName();
				offset = getProcedureOffset();

				if (oldProcName != null) {
					frames.Add(new SDCDCombinedStackFrame("", oldProcName, oldOffset, oldIp, oldSp, ip, 0, null));
				}
				oldIp = ip;
				oldSp = sp;
				oldOffset = offset;
				oldProcName = procName;
			} while (!step() && ++nFrames < MAX_FRAMES);

			thread.StackTrace = new SDCombinedStackTrace(frames);
		}

		private void SetAuxvFields(SDCDSystemContext context) {
			// get system architecture directly from the header instead of using libunwind
			// currently, libunwind only works for 64bit dumps.
			context.SystemArchitecture = GetSystemArchitecture();
			context.PageSize = (int)getAuxvValue(AuxType.PAGE_SIZE.Type);
			context.EntryPoint = getAuxvValue(AuxType.ENTRY_POINT.Type);
			context.BasePlatform = getAuxvString(AuxType.BASE_PLATFORM.Type);
			context.Uid = (int)getAuxvValue(AuxType.UID.Type);
			context.Euid = (int)getAuxvValue(AuxType.EUID.Type);
			context.Gid = (int)getAuxvValue(AuxType.GID.Type);
			context.Egid = (int)getAuxvValue(AuxType.EGID.Type);
		}

		private string GetSystemArchitecture() {
			int arch = is64Bit();
			if(arch == 0) {
				return "x86";
			} else if(arch == 1) {
				return "Amd64";
			} else {
				Console.WriteLine($"Invalid system architecture {arch}!");
				return "N/A";
			}
		}
	}
}
