using SuperDump.Analyzer.Linux.Boundary;
using SuperDump.Models;
using SuperDumpModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Thinktecture.IO;
using Thinktecture.IO.Adapters;

namespace SuperDump.Analyzer.Linux.Analysis {
    public class DebugSymbolAnalysis {

		private static Regex addr2lineRegex = new Regex("([^:]+):(\\d+)", RegexOptions.Compiled);

		private readonly IFilesystem filesystem;
		private readonly IProcessHandler processHandler;

		private readonly SDResult analysisResult;

		public DebugSymbolAnalysis(IFilesystem filesystem, IProcessHandler processHandler, SDResult result) {
			this.filesystem = filesystem ?? throw new ArgumentNullException("Filesystem must not be null!");
			this.processHandler = processHandler ?? throw new ArgumentNullException("ProcessHandler must not be null!");
			this.analysisResult = result ?? throw new ArgumentNullException("SD Result must not be null!");
		}

		public void Analyze() {
			if(this.analysisResult?.ThreadInformation == null) {
				throw new ArgumentNullException("Debug symbol analysis can only be executed when thread information is set!");
			}
			if(this.analysisResult?.SystemContext?.Modules == null) {
				throw new ArgumentNullException("Debug symbol analysis can only be executed when modules are set!");
			}
			AnalyzeChecked();
		}

		private void AnalyzeChecked() {
			IEnumerable<Task> tasks = StartSourceRetrievalTasks();
			foreach(Task t in tasks) {
				t.Wait();
			}
		}

		private IEnumerable<Task> StartSourceRetrievalTasks() {
			foreach (var threadInfo in this.analysisResult.ThreadInformation) {
				foreach (var stackFrame in threadInfo.Value.StackTrace) {
					SDCDModule module = FindModuleAtAddress(this.analysisResult.SystemContext.Modules, stackFrame.InstructionPointer);
					if (module?.LocalPath != null) {
						stackFrame.ModuleName = module.FileName;
						yield return AddSourceInfoAsync(stackFrame, module);
					}
				}
			}
		}

		private async Task AddSourceInfoAsync(SDCombinedStackFrame stackFrame, SDCDModule module) {
			Tuple<SDFileAndLineNumber, string> methodSource = await Address2MethodSourceAsync(stackFrame.InstructionPointer, module);
			SDFileAndLineNumber sourceInfo = methodSource.Item1;
			string methodName = methodSource.Item2;
			if (methodName != "??") {
				stackFrame.MethodName = methodName;
				if (sourceInfo.File != null && sourceInfo.File != "??") {
					stackFrame.SourceInfo = sourceInfo;
				}
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

		private async Task<Tuple<SDFileAndLineNumber, string>> Address2MethodSourceAsync(ulong instrPtr, SDCDModule module) {
			ulong relativeIp = instrPtr;
			var systemContext = (SDCDSystemContext)analysisResult.SystemContext;

			// Calculate the relative IP to the base module. 
			// For this we must subtract the start address of the whole module from the IP
			// The module start address from the parameter is only the start address of the segment.
			// We get the real module start address by subtracting the page offset multiplied by the page size from the segment start.
			ulong moduleStartAddress = module.StartAddress - (module.Offset * (uint)systemContext.PageSize);
			relativeIp -= moduleStartAddress + 1;

			string output = await processHandler.ExecuteProcessAndGetOutputAsync("addr2line", $"-f -C -e {module.LocalPath} 0x{relativeIp.ToString("X")}");
			string[] lines = output.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
			if(lines.Length < 2) {
				Console.WriteLine($"Output of addr2line is invalid ({lines.Length} lines)! First line: {lines?[0]}");
				return Tuple.Create<SDFileAndLineNumber, string>(new SDFileAndLineNumber(), null);
			}
			string methodName = lines[0];
			string fileLine = lines[1];
			SDFileAndLineNumber sourceInfo = RetrieveSourceInfo(fileLine);
			return Tuple.Create(sourceInfo, methodName);
		}

		private SDFileAndLineNumber RetrieveSourceInfo(string output) {
			Match match = addr2lineRegex.Match(output);
			if(match.Success) {
				SDFileAndLineNumber sourceInfo = new SDFileAndLineNumber() {
					File = match.Groups[1].Value,
					Line = int.Parse(match.Groups[2].Value)
				};
				return sourceInfo;
			}
			return new SDFileAndLineNumber();
		}
	}
}
