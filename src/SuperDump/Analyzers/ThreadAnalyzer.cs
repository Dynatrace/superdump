using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;
using SuperDump.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SuperDumpModels;
using SuperDump.ModelHelpers;

namespace SuperDump.Analyzers {
	public class ThreadAnalyzer {
		public uint numberOfThreads;
		private DumpContext context;
		public Dictionary<uint, SDThread> threads;
		public List<SDClrException> exceptions;
		public List<SDDeadlockContext> deadlocks;
		public Dictionary<int, string> threadNames;

		private IDebugClient debugClient;

		public ThreadAnalyzer(DumpContext context) {
			this.context = context;
			this.exceptions = new List<SDClrException>();
			this.threads = new Dictionary<uint, SDThread>();
			this.deadlocks = new List<SDDeadlockContext>();

			using (DataTarget target = this.context.CreateTemporaryDbgEngTarget()) {
				this.debugClient = target.DebuggerInterface;

				// init threads
				this.InitAllThreadIds();
				this.InitBasicThreadInformation();
				this.GetCombinedStackTraces();
				this.GetManagedExceptions();
				this.GetBlockingObjects();
				this.DetectDeadlock();
			}
		}

		private void GetBlockingObjects() {
			if (this.context.Runtime != null && this.context.Heap != null && this.context.Heap.CanWalkHeap) {
				foreach (ClrThread thread in context.Runtime.Threads) {
					foreach (BlockingObject obj in thread.BlockingObjects) {
						if (obj != null) {
							this.threads[thread.OSThreadId].BlockingObjects.Add(obj.ToSDModel());
						}
					}
				}
			} else {
				context.WriteWarning("Blocking objects could not be obtained, either there is no CLR in process, or no heap information.");
			}
		}

		public void PrintManagedCallStacks() {
			//Threads test, get info of all managed threads found in dump 
			context.WriteInfo("--- Managed callstacks ---");
			if (context.Runtime != null) {
				foreach (ClrThread thread in context.Runtime.Threads) {
					if (!thread.IsAlive)
						continue;

					context.WriteInfo("Thread        {0}, managed id: {1}:", thread.OSThreadId, thread.ManagedThreadId);
					context.WriteInfo("GC mode:      {0} ", thread.GcMode);
					context.WriteInfo("Thread type:  {0}", thread);
					context.WriteInfo("Callstack: {0:X} - {1:X}", thread.StackBase, thread.StackLimit);

					// get last thrown exception of thread
					ClrException lastException = thread.CurrentException;
					if (lastException != null) {
						this.PrintException(lastException);
					}
					// walk each stack frame
					foreach (ClrStackFrame frame in thread.StackTrace) {
						SDFileAndLineNumber info = frame.GetSourceLocation();
						context.WriteLine("{0,12:X} {1,12:X} {2} - {3}:{4}", frame.StackPointer, frame.InstructionPointer, frame.DisplayString, info.File, info.Line);
					}
					context.WriteLine(null);
				}
				context.WriteInfo("Total amount of all (managed) threads: " + context.Runtime.Threads.Count);
			}
		}

		/// <summary>
		/// Inits the thread dictionary with OS ID and engine ID of every thread,
		/// and also refs managed thread if there is one for each thread in the process dump
		/// </summary>
		private void InitAllThreadIds() {
			// get amount of threads in the process
			Utility.CheckHRESULT(((IDebugSystemObjects)this.debugClient).GetNumberThreads(out numberOfThreads));

			for (uint i = 0; i < numberOfThreads; i++) {
				var engineThreadIds = new uint[1]; // has to be array, see IDebugSystemObjects.GetThreadIdsByIndex
				var osThreadIds = new uint[1]; // same here

				// get the engine id(s) and also the os id(s) of each thread
				Utility.CheckHRESULT(((IDebugSystemObjects)this.debugClient).GetThreadIdsByIndex(i, 1, engineThreadIds, osThreadIds));

				// create new SDThread object and add it to the dictionary
				var t = new SDThread(i);
				t.OsId = osThreadIds[0];
				t.EngineId = engineThreadIds[0];

				ClrThread managedThread = null;
				if (context.Runtime != null) {
					managedThread = context.Runtime.Threads.FirstOrDefault(thread => thread.OSThreadId == t.OsId);
					if (managedThread != null) {
						t.IsManagedThread = true;
						t.ManagedThreadId = managedThread.ManagedThreadId;
						t.State = managedThread.SpecialDescription();
						t.IsThreadPoolThread = managedThread.IsThreadPoolThread();
					} else {
						t.IsManagedThread = false;
					}
				}

				this.threads.Add(t.OsId, t);
			}
		}

		/// <summary>
		/// inits threads with timing information and state information
		/// </summary>
		private void InitBasicThreadInformation() {
			this.threadNames = GetManagedThreadNames(context.Heap);

			foreach (SDThread thread in this.threads.Values) {
				//get managed thread name, if there is one
				if (thread.IsManagedThread) {
					string name;
					threadNames.TryGetValue(thread.ManagedThreadId, out name);
					if (!string.IsNullOrEmpty(name)) {
						thread.ThreadName = name;
					}
				}

				int size = Marshal.SizeOf(typeof(DEBUG_THREAD_BASIC_INFORMATION));
				var buffer = new byte[size];
				if (((IDebugAdvanced2)this.debugClient).
					GetSystemObjectInformation(DEBUG_SYSOBJINFO.THREAD_BASIC_INFORMATION, 0, thread.EngineId, buffer, buffer.Length, out size) >= 0) {

					// create GCHandle for the managed object 'buffer', because we need a pointer for the DEBUG_... struct!
					GCHandle gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
					try {
						// now get the ptr for the DEBUG_.. struct with the now pinned address of buffer
						var basicInfo = (DEBUG_THREAD_BASIC_INFORMATION)Marshal.PtrToStructure(gcHandle.AddrOfPinnedObject(), typeof(DEBUG_THREAD_BASIC_INFORMATION));

						// always check if valid with corresponding bit flag
						if ((basicInfo.Valid & DEBUG_TBINFO.TIMES) != 0) {
							thread.CreationTime = basicInfo.CreateTime;
							thread.ExitTime = basicInfo.ExitTime;
							thread.KernelTime = basicInfo.KernelTime;
							thread.UserTime = basicInfo.UserTime;
						}
						if ((basicInfo.Valid & DEBUG_TBINFO.PRIORITY) != 0) {
							thread.Priority = basicInfo.Priority;
						}
						if ((basicInfo.Valid & DEBUG_TBINFO.PRIORITY_CLASS) != 0) {
							thread.PriorityClass = basicInfo.PriorityClass;
						}
						if ((basicInfo.Valid & DEBUG_TBINFO.EXIT_STATUS) != 0) {
							thread.ExitStatus = basicInfo.ExitStatus;
						}
						if ((basicInfo.Valid & DEBUG_TBINFO.START_OFFSET) != 0) {
							thread.StartOffset = basicInfo.StartOffset;
						}
					} finally {
						gcHandle.Free(); // free pinned object now
					}
				}
			}
		}

		private Dictionary<int, string> GetManagedThreadNames(ClrHeap heap) {
			var result = new Dictionary<int, string>();
			if (heap == null || !heap.CanWalkHeap)
				return result;

			var threadObjects = from obj in heap.EnumerateObjectAddresses()
								let type = heap.GetObjectType(obj)
								where type != null && type.Name == "System.Threading.Thread"
								select obj;
			ClrType threadType = heap.GetTypeByName("System.Threading.Thread");
			ClrInstanceField nameField = threadType.GetFieldByName("m_Name");
			ClrInstanceField managedIdField = threadType.GetFieldByName("m_ManagedThreadId");

			foreach (var threadObject in threadObjects) {
				string name = (string)nameField.GetValue(threadObject);
				int id = (int)managedIdField.GetValue(threadObject);
				result[id] = name;
			}

			return result;
		}

		/// <summary>
		/// gets stack traces for each thread
		/// </summary>
		private void GetCombinedStackTraces() {
			// get stacktrace of each thread
			// ((IDebugSymbols2)this.debugClient).SetSymbolOptions(SYMOPT.LOAD_ANYTHING);
			foreach (SDThread thread in this.threads.Values) {
				var trace = new CombinedStackTrace(thread.EngineId, thread.OsId, thread.IsManagedThread, this.debugClient, this.context);
				// wrap to SDCombinedStackTrace
				IList<SDCombinedStackFrame> t = new List<SDCombinedStackFrame>();
				ulong lastStackPtr = 0;
				int i = 0;
				foreach (CombinedStackFrame frame in trace) {
					ulong stackPtrOffset;
					if (lastStackPtr == 0) {
						stackPtrOffset = 0; // think about diffing with rsp register
					} else {
						stackPtrOffset = frame.StackPointer - lastStackPtr;
					}

					var newFrame = new SDCombinedStackFrame(frame.Type,
						frame.ModuleName,
						frame.MethodName,
						frame.OffsetInMethod,
						frame.InstructionPointer,
						frame.StackPointer,
						frame.ReturnOffset,
						stackPtrOffset,
						frame.SourceInfo);
					if (frame.LinkedStackFrame != null) {
						newFrame.LinkedStackFrame = new SDCombinedStackFrame(frame.LinkedStackFrame.Type,
							frame.LinkedStackFrame.ModuleName,
							frame.LinkedStackFrame.MethodName,
							frame.LinkedStackFrame.OffsetInMethod,
							frame.LinkedStackFrame.InstructionPointer,
							frame.LinkedStackFrame.StackPointer,
							frame.LinkedStackFrame.ReturnOffset,
							stackPtrOffset,
							frame.SourceInfo);
					}
					lastStackPtr = frame.StackPointer;
					t.Add(newFrame);
					i++;
				}
				thread.StackTrace = new SDCombinedStackTrace(t);
			}
		}

		private void GetManagedExceptions() {
			if (context.Runtime != null) {
				foreach (ClrThread thread in this.context.Runtime.Threads) {
					if (thread.CurrentException != null) {
						if (this.threads[thread.OSThreadId] != null) {
							var ex = thread.CurrentException.ToSDModel();
							ex.OSThreadId = thread.OSThreadId;
							this.threads[thread.OSThreadId].LastException = ex;
							this.exceptions.Add(ex);
						}
						// else something went wrong with initialization?
						else {
							throw new Exception("Thread with OS ID {" + thread.OSThreadId +
								"} was not found in dictionary! Something went wrong when initializing.");
						}
					}
				}
			}
		}

		public void PrintManagedExceptions() {
			if (context.Runtime != null) {
				foreach (ClrThread thread in this.context.Runtime.Threads) {
					ClrException lastException = thread.CurrentException;
					if (lastException != null) {
						context.Write("Exception on thread {0} (OS:{1}): ",
							thread.ManagedThreadId,
							thread.OSThreadId);
						this.PrintException(lastException);
						if (lastException.Inner != null)
							this.PrintException(lastException.Inner);
					}
				}
			}
		}

		private void PrintException(ClrException exception) {
			if (exception != null) {
				context.WriteLine("Address: {0:X}, Type: {1}, Message: {2}",
					exception.Address,
					exception.Type.Name,
					exception.GetExceptionMessageSafe());
				context.WriteLine("Stacktrace:");
				foreach (ClrStackFrame frame in exception.StackTrace) {
					context.WriteLine("{0,-20:x16} {1}!{2}",
						frame.InstructionPointer,
						frame.ModuleName,
						frame.DisplayString);
				}
			}
		}

		public uint GetLastExecutedThreadEngineId() {
			using (DataTarget target = this.context.CreateTemporaryDbgEngTarget()) {
				uint id;
				Utility.CheckHRESULT(((IDebugSystemObjects)target.DebuggerInterface).GetCurrentThreadId(out id));
				return id;
			}
		}

		public uint GetLastExecutedThreadOSId() {
			uint engineId = GetLastExecutedThreadEngineId();
			return this.threads.First(t => t.Value.EngineId == engineId).Key;
		}

		public void PrintCompleteStackTrace() {
			context.WriteInfo("--- Mixed stacktraces ---");

			foreach (SDThread t in this.threads.Values) {
				this.PrintStackTrace(t);
			}
		}

		public void PrintStackTrace(SDThread thread) {
			context.WriteInfo("Thread CLR ID: {0}, OS ID: {1:X}, State: {2}, IsThreadPoolThread: {3}, {4}", thread.EngineId, thread.OsId, thread.State, thread.IsThreadPoolThread, TagAnalyzer.TagsAsString("Tags: ", thread.Tags));
			foreach (SDCombinedStackFrame frame in thread.StackTrace) {
				if (frame.Type == StackFrameType.Special) {
					context.WriteLine("{0,-10} {1,-20:x16} {2}", "Special", frame.InstructionPointer, "[" + frame.MethodName + "]");
				} else {
					context.WriteLine("{0,-10} {1,-20:x16} {2}!{3}+0x{4:x}",
						frame.Type,
						frame.InstructionPointer,
						frame.ModuleName,
						frame.MethodName,
						frame.OffsetInMethod);
					Console.ResetColor();
				}
			}
			context.WriteInfo("-- end call stack (thread {0}) --\n", thread.OsId);
		}

		public void PrintThreadPool() {
			context.WriteInfo(" --- CLR Thread pool info ---");

			PrintThreadPoolInfo();
		}

		public void DetectDeadlock() {
			context.WriteLine("--- Deadlock detection ---");
			if (context.Runtime != null && context.Heap != null && context.Heap.CanWalkHeap) {
				foreach (ClrThread thread in context.Runtime.Threads) {
					this.PrintChainForThread(thread);
				}
			} else {
				context.WriteWarning("Deadlock detection is disabled, either there is no CLR in process or no heap information.");
			}
		}

		public void PrintChainForThread(ClrThread thread) {
			this.PrintChainForThreadHelper(thread, 0, 0, new HashSet<uint>());
		}

		private void PrintChainForThreadHelper(ClrThread thread, int depth, uint lastVisitedThread, HashSet<uint> visitedThreads) {
			context.WriteLine("{0} Thread {1}", new string(' ', depth * 2), thread.ManagedThreadId);

			if (visitedThreads.Contains(thread.OSThreadId)) {
				this.deadlocks.Add(new SDDeadlockContext(visitedThreads, lastVisitedThread, thread.OSThreadId));
				context.WriteInfo("{0}*** Deadlock detected ***", new string(' ', depth * 2));
				return;
			}
			// add visited thread
			visitedThreads.Add(thread.OSThreadId);

			foreach (BlockingObject obj in thread.BlockingObjects) {
				context.Write("{0}|{1}", new string(' ', (depth + 1) * 2), obj.Reason);
				ClrType type = this.context.Heap.GetObjectType(obj.Object);
				if (type != null && !string.IsNullOrEmpty(type.Name)) {
					context.WriteLine("{0:x16} {1}", obj.Object, type.Name);
				} else { // object type could not be resolved, just print reason (monitor, etc.)
					context.WriteLine("{0:x16}", obj.Reason);
				}
				foreach (ClrThread owner in obj.Owners) {
					if (owner != null) {
						PrintChainForThreadHelper(owner, depth + 2, thread.OSThreadId, visitedThreads);
					}
				}
			}
		}

		private void PrintThreadPoolInfo() {
			if (context.Runtime != null) {
				ClrThreadPool threadPool = this.context.Runtime.GetThreadPool();

				if (threadPool != null) {
					context.WriteLine("Total threads:     {0}", threadPool.TotalThreads);
					context.WriteLine("Running threads:   {0}", threadPool.RunningThreads);
					context.WriteLine("Max threads:       {0}", threadPool.MaxThreads);
					context.WriteLine("Min threads:       {0}", threadPool.MinThreads);
					context.WriteLine("Idle threads:      {0}", threadPool.IdleThreads);
					context.WriteLine("CPU utilization    {0}% (estimated)", threadPool.CpuUtilization);
				}
			}
		}
	}
}
