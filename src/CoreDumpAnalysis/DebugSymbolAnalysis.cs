using SuperDump.Models;
using SuperDumpModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CoreDumpAnalysis
{
    class DebugSymbolAnalysis {
		private readonly String coredump;
		private readonly SDResult analysisResult;

		public DebugSymbolAnalysis(String coredump, SDResult result) {
			this.analysisResult = result ?? throw new ArgumentNullException("SD Result must not be null!");
			this.coredump = coredump ?? throw new ArgumentNullException("Coredump Path must not be null!");
		}

		public void DebugAndSetResultFields() {
			if(this.analysisResult?.ThreadInformation == null) {
				Console.WriteLine("Cannot add information from debug symbols because there is no thread information present!");
				return;
			}
			if(this.analysisResult?.SystemContext?.Modules == null) {
				Console.WriteLine("Cannot add information from debug symbols because there is no system context information present!");
				return;
			}
			Analyze();
		}

		private void Analyze() {
			foreach (var threadInfo in this.analysisResult.ThreadInformation) {
				foreach (var stackFrame in threadInfo.Value.StackTrace) {
					SDCDModule module = FindModuleAtAddress(this.analysisResult.SystemContext.Modules, stackFrame.InstructionPointer);
					if (module != null) {
						stackFrame.ModuleName = module.FileName;
						AddSourceInfo(stackFrame, module);
					}
				}
			}
		}

		private void AddSourceInfo(SDCombinedStackFrame stackFrame, SDCDModule module) {
			Tuple<SDFileAndLineNumber, string> methodSource = Address2MethodSource(stackFrame.InstructionPointer, module);
			SDFileAndLineNumber sourceInfo = methodSource.Item1;
			string methodName = methodSource.Item2;
			if (methodName != "??") {
				stackFrame.MethodName = methodName;
				stackFrame.SourceInfo = sourceInfo;
			}
		}

		private SDCDModule FindModuleAtAddress(IList<SDModule> modules, ulong instrPtr) {
			foreach (SDModule module in modules) {
				if (module.GetType() != typeof(SDCDModule)) {
					throw new InvalidCastException("Plain SDModule found in module list. SDCDModule expected.");
				}
				SDCDModule cdModule = (SDCDModule)module;
				if (cdModule.StartAddress < instrPtr && cdModule.EndAddress > instrPtr) {
					return cdModule;
				}
			}
			return null;
		}

		private Tuple<SDFileAndLineNumber, string> Address2MethodSource(ulong instrPtr, SDCDModule module) {
			ulong relativeIp = instrPtr - module.StartAddress;

			var process = new Process {
				StartInfo = new ProcessStartInfo {
					FileName = "addr2line",
					Arguments = "-f -C -e " + module.LocalPath + " 0x" + relativeIp.ToString("X"),
					UseShellExecute = false,
					RedirectStandardOutput = true,
					CreateNoWindow = true
				}
			};
			process.Start();
			string dbg = module.LocalPath.Substring(0, module.LocalPath.Length - 2) + "dbg";
			string methodName = process.StandardOutput.ReadLine();
			string fileLine = process.StandardOutput.ReadLine();
			SDFileAndLineNumber sourceInfo = RetrieveSourceInfo(fileLine);
			return Tuple.Create(sourceInfo, methodName);
		}

		private SDFileAndLineNumber RetrieveSourceInfo(string output) {
			int lastColon = output.LastIndexOf(':');
			if (lastColon > 0) {
				SDFileAndLineNumber sourceInfo = new SDFileAndLineNumber();
				sourceInfo.File = output.Substring(0, lastColon);
				string sLine = output.Substring(lastColon + 1);
				if (!Int32.TryParse(sLine, out sourceInfo.Line)) {
					sourceInfo.Line = 0;
				}
				return sourceInfo;
			}
			return null;
		}
	}
}
