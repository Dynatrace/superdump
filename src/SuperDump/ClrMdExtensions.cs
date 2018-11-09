using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Utilities;
using Microsoft.Diagnostics.Runtime.Utilities.Pdb;
using MoreLinq;
using SuperDumpModels;
using System;
using System.Collections.Generic;
using System.IO;

namespace SuperDump {
	public static class ClrMdExtensions {
		/// <summary>
		/// Creates and returns ClrRuntime based on dump file (takes first loaded CLR if loaded more than one)
		/// Loads dac from public MS symbol server if dac is not locally available, or add path to dac as param
		/// </summary>
		/// <param name="dac">path of the dac</param>
		/// <returns>Returns the created runtime object</returns>
		public static ClrRuntime CreateRuntime(this DataTarget target, ref string dac) {
			// now check bitness of our program/target, defensive check if Selector was not used or was wrong
			bool isTarget64Bit = target.PointerSize == 8;
			if (Environment.Is64BitProcess != isTarget64Bit) { //check if our process is 64bit or not
				throw new InvalidOperationException(string.Format("Architecture do no match:  Process was {0} but target is {1} !", Environment.Is64BitProcess ? "64 bit" : "32 bit", isTarget64Bit ? "64 bit" : "32 bit"));
			}

			// Get "highest" CLR loaded and use that
			if (target.ClrVersions == null || target.ClrVersions.Count <= 0) {
				throw new FileNotFoundException("No CLR was loaded in that process!");
			}
			ClrInfo version = target.ClrVersions.MaxBy(v => v.Version.ToString()).First();

			// try to make sure we have the right Dac to load.  Note we are doing this manually for
			// illustration.  Simply calling version.CreateRuntime with no arguments does the same steps.
			if (dac != null && File.Exists(dac)) {
				dac = Path.Combine(dac, version.DacInfo.FileName);
			} else if (dac == null || !File.Exists(dac)) {
				//dac = target.SymbolLocator.FindBinary(version.DacInfo);
				dac = target.SymbolLocator.FindBinary(version.DacInfo);
			}

			// finally, check to see if the dac exists.  If not, throw an exception.
			if (dac == null || !File.Exists(dac)) {
				throw new FileNotFoundException("Could not find the specified dac.", dac);
			}

			// now that we have the DataTarget, the version of CLR, and the right dac, create and return a
			// ClrRuntime instance.
			return version.CreateRuntime(dac, true);
		}

		public static bool IsThreadPoolThread(this ClrThread thread) {
			return thread.IsThreadpoolCompletionPort || thread.IsThreadpoolGate || thread.IsThreadpoolTimer || thread.IsThreadpoolWait || thread.IsThreadpoolWorker;
		}

		public static string ApartmentDescription(this ClrThread thread) {
			if (thread.IsMTA)
				return "MTA";
			if (thread.IsSTA)
				return "STA";
			return "None";
		}

		public static string SpecialDescription(this ClrThread thread) {
			string states = string.Empty;

			if (thread.IsAlive) {
				states += "Alive";
			}
			if (thread.IsDebuggerHelper) {
				states += "DbgHelper";
			}
			if (thread.IsFinalizer) {
				states += "Finalizer";
			}
			if (thread.IsGC) {
				states += "GC";
			}
			if (thread.IsShutdownHelper) {
				states += "ShutdownHelper";
			}
			if (thread.IsAborted) {
				states += "Aborted";
			}
			if (thread.IsAbortRequested) {
				states += "AbortRequested";
			}
			if (thread.IsUnstarted) {
				states += "Unstarted";
			}
			if (thread.IsUserSuspended) {
				states += "Suspended";
			}
			if (string.IsNullOrEmpty(states)) {
				states += "unknown";
			}
			return states;
		}
	}

	/// <summary>
	/// from https://github.com/Microsoft/clrmd/blob/master/src/FileAndLineNumbers/Program.cs
	/// </summary>
	internal static class PdbReaderHelperExtensions {
		private static Dictionary<PdbInfo, PdbReader> s_pdbReaders = new Dictionary<PdbInfo, PdbReader>();

		public static SDFileAndLineNumber GetSourceLocation(this ClrStackFrame frame) {
			try {
				PdbReader reader = GetReaderForFrame(frame);
				if (reader == null) {
					return null;
				}

				PdbFunction function = reader.GetFunctionFromToken(frame.Method.MetadataToken);
				int ilOffset = FindIlOffset(frame);

				return FindNearestLine(function, ilOffset);
			} catch (Exception e) {
				Console.WriteLine($"exception in {nameof(GetSourceLocation)}: {e}");
				return null;
			}
		}

		private static SDFileAndLineNumber FindNearestLine(PdbFunction function, int ilOffset) {
			int distance = int.MaxValue;
			var nearest = new SDFileAndLineNumber();

			foreach (PdbSequencePointCollection sequenceCollection in function.SequencePoints) {
				foreach (PdbSequencePoint point in sequenceCollection.Lines) {
					int dist = (int)Math.Abs(point.Offset - ilOffset);
					if (dist < distance) {
						nearest.File = sequenceCollection.File.Name;
						nearest.Line = (int)point.LineBegin;
					}
				}
			}

			return nearest;
		}

		private static int FindIlOffset(ClrStackFrame frame) {
			ulong ip = frame.InstructionPointer;
			int last = -1;
			foreach (ILToNativeMap item in frame.Method.ILOffsetMap) {
				if (item.StartAddress > ip) {
					return last;
				}

				if (ip <= item.EndAddress) {
					return item.ILOffset;
				}
				last = item.ILOffset;
			}

			return last;
		}

		private static PdbReader GetReaderForFrame(ClrStackFrame frame) {
			ClrModule module = frame.Method?.Type?.Module;
			PdbInfo info = module?.Pdb;

			PdbReader reader = null;
			if (info != null) {
				if (!s_pdbReaders.TryGetValue(info, out reader)) {
					SymbolLocator locator = GetSymbolLocator(module);
					string pdbPath = locator.FindPdb(info);
					if (pdbPath != null) {
						reader = new PdbReader(pdbPath);
					}

					s_pdbReaders[info] = reader;
				}
			}

			return reader;
		}

		private static SymbolLocator GetSymbolLocator(ClrModule module) {
			return module.Runtime.DataTarget.SymbolLocator;
		}
	}
}
