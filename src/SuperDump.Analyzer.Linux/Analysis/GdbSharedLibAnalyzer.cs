using SuperDump.Analyzer.Linux.Boundary;
using SuperDump.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Thinktecture.IO;

namespace SuperDump.Analyzer.Linux.Analysis {
	/// <summary>
	/// This class uses GDB to retrieve shared library information. This process is only required for dumps that were generated 
	/// from kernel versions below 3.6 and therefore don't contain the NT_FILE note which contains the information in a more 
	/// compact format.
	/// The addresses taken from GDB must be subtracted with the offset address of the .text section. Readelf is used for to
	/// retrieve this offset. Finally, the address for the main executable is retrieved from the first LOAD entry in the program
	/// header section.
	/// The libunwind wrapper must be initialized before calling this class.
	/// </summary>
	public class GdbSharedLibAnalyzer {

		private static readonly Regex ReadelfSectionRegex = new Regex(@"\.\w+\s*PROGBITS\s*([\da-f]+)\s*([\da-f]+)", RegexOptions.Compiled);
		private static readonly Regex ProgramHeaderLoadRegex = new Regex(@"LOAD\s+0x(?:[\da-f]+)\s+0x([\da-f]+)", RegexOptions.Compiled);
		private static readonly Regex GdbLibraryRegex = new Regex(@"0x([\da-f]+)\s+0x([\da-f]+)\s+\w*\s+(?:\(\*\)\s+)?(.*)", RegexOptions.Compiled);

		[DllImport(Constants.WRAPPER)]
		private static extern void addBackingFileAtAddr(string filepath, ulong address);

		private readonly IFilesystem filesystem;
		private readonly IProcessHandler processHandler;

		private readonly IFileInfo coredump;
		private readonly SDCDSystemContext systemContext;

		public GdbSharedLibAnalyzer(IFilesystem filesystem, IProcessHandler processHandler, IFileInfo coredump, SDResult result) {
			this.filesystem = filesystem ?? throw new ArgumentNullException("FilesystemHelper must not be null!");
			this.processHandler = processHandler ?? throw new ArgumentNullException("ProcessHandler must not be null!");
			this.coredump = coredump ?? throw new ArgumentNullException("Coredump must not be null!");

			this.systemContext = (SDCDSystemContext)result.SystemContext;
			this.systemContext.Modules = new List<SDModule>();
		}

		public void Analyze() {
			try {
				using (ProcessStreams stream = processHandler.StartProcessAndReadWrite("gdb", "")) {
					Task<string> outReader = stream.Output.ReadToEndAsync();
					Task<string> errReader = stream.Error.ReadToEndAsync();
					SendCommandsToGdb(stream.Input);
					if (!outReader.Wait(TimeSpan.FromMinutes(1))) {
						Console.WriteLine("Retrieving shared library info from GDB timed out!");
						return;
					}
					string output = outReader.Result;
					string error = errReader.Result;
					AnalyzeGdbOutputAsync(output, error).Wait();
				}
			} catch (ProcessStartFailedException e) {
				Console.WriteLine($"Failed to start GDB: {e.ToString()}");
				return;
			}
		}

		private void SendCommandsToGdb(StreamWriter input) {
			input.WriteLine("set solib-absolute-prefix .");
			if (systemContext.FileName != null) {
				input.WriteLine("file " + systemContext.FileName);
			}
			input.WriteLine("core-file " + coredump.FullName);
			input.WriteLine("info shared");
			input.WriteLine("quit");
		}

		private async Task AnalyzeGdbOutputAsync(string gdbOut, string gdbErr) {
			IList<SDModule> modules = systemContext.Modules;
			if (gdbErr != null && gdbErr != "") {
				Console.WriteLine("GDB Error: " + gdbErr);
			}
			ExtractModules(gdbOut, modules);

			await ResolveSymlinks(modules);
			await AddBackingFiles(modules);
			await AddMainExecutable();
		}

		private void ExtractModules(string gdbOut, IList<SDModule> modules) {
			foreach (string line in gdbOut.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)) {
				Match match = GdbLibraryRegex.Match(line);
				if (match.Success) {
					string startAddr = match.Groups[1].Value;
					string endAddr = match.Groups[2].Value;
					string library = match.Groups[3].Value;
					Console.WriteLine("Shared Library: 0x" + startAddr + " - 0x" + endAddr + ": " + library);

					modules.Add(new SDCDModule() {
						StartAddress = Convert.ToUInt64(startAddr, 16),
						EndAddress = Convert.ToUInt64(endAddr, 16),
						FilePath = library,
						FileName = Path.GetFileName(library),
						LocalPath = filesystem.GetFile(library).FullName,
						FileSize = (uint)GetFileSizeForLibrary(library)
					});
				}
			}
		}

		private async Task ResolveSymlinks(IList<SDModule> modules) {
			foreach (SDModule module in modules) {
				SDCDModule cdModule = (SDCDModule)module;
				string output = await processHandler.ExecuteProcessAndGetOutputAsync("readlink", "-f " + cdModule.LocalPath);
				string path = output.Trim();
				cdModule.LocalPath = path;
				cdModule.FileName = Path.GetFileName(path);
			}
		}

		private async Task AddBackingFiles(IList<SDModule> modules) {
			foreach (SDModule module in modules) {
				SDCDModule cdModule = (SDCDModule)module;
				string output = await processHandler.ExecuteProcessAndGetOutputAsync("readelf", "-S " + cdModule.LocalPath);

				foreach (string line in output.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)) {
					if (line.Contains(".text")) {
						Match match = ReadelfSectionRegex.Match(line);
						if (match.Success) {
							ulong textOffset = Convert.ToUInt64(match.Groups[2].Value, 16);
							cdModule.StartAddress -= textOffset;
							break;
						}
					}
				}
				addBackingFileAtAddr(cdModule.LocalPath, cdModule.StartAddress);
			}
		}

		private async Task AddMainExecutable() {
			string mainExecutable = systemContext.FileName;
			string execOutput = await processHandler.ExecuteProcessAndGetOutputAsync("readelf", "-l " + mainExecutable);
			foreach (string line in execOutput.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)) {
				Match match = ProgramHeaderLoadRegex.Match(line);
				if (match.Success) {
					ulong address = Convert.ToUInt64(match.Groups[1].Value, 16);
					addBackingFileAtAddr(mainExecutable, address);
					break;
				}
			}
		}

		private long GetFileSizeForLibrary(string filepath) {
			IFileInfo file = filesystem.GetFile($".{filepath}");
			if (!file.Exists) {
				file = filesystem.GetFile(filepath);
				if (!file.Exists) {
					return 0;
				}
			}
			return file.Length;
		}
	}
}
