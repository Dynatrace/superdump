using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;
using Newtonsoft.Json;
using SuperDump.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SuperDump.ModelHelpers;

namespace SuperDump.Analyzers {
	public class SystemAnalyzer {
		public SDSystemContext systemInfo;
		private IDebugClient debugClient;
		private DumpContext context;

		public SystemAnalyzer(DumpContext context) {
			this.context = context;
			this.systemInfo = new SDSystemContext();
			using (DataTarget t = this.context.CreateTemporaryDbgEngTarget()) {
				this.debugClient = t.DebuggerInterface;
				this.InitSystemInfo();
			}
		}

		private void InitSystemInfo() {
			this.systemInfo.OSVersion = this.GetOSVersion();
			this.systemInfo.Modules = this.GetModuleList();
			this.systemInfo.SystemUpTime = this.GetSystemUpTime();
			this.systemInfo.ProcessUpTime = this.GetProcessUpTime();
			this.systemInfo.NumberOfProcessors = this.GetNumberOfProcessors();
			this.systemInfo.ClrVersions = this.GetClrVersions();
			this.systemInfo.AppDomains = this.GetAppDomains();
			this.systemInfo.SharedDomain = this.GetSharedDomain();
			this.systemInfo.SystemDomain = this.GetSystemDomain();
			this.systemInfo.DumpTime = this.GetDumpTime();
			this.systemInfo.ProcessArchitecture = this.GetProcessArchitecture();
			this.systemInfo.SystemArchitecture = this.GetProcessorType();
		}

		public string SerializeSystemInfoToJSON() {
			string json = JsonConvert.SerializeObject(this.systemInfo, Formatting.Indented, new JsonSerializerSettings {
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			});

			return json;
		}

		private string GetProcessArchitecture() {
			return this.context.Target.Architecture.ToString();
		}

		private SDAppDomain GetSharedDomain() {
			if (this.context.Runtime != null) {
				return this.context.Runtime.SharedDomain.ToSDModel();
			} else {
				return null;
			}
		}

		private SDAppDomain GetSystemDomain() {
			if (this.context.Runtime != null) {
				return this.context.Runtime.SystemDomain.ToSDModel();
			} else {
				return null;
			}
		}

		private string GetProcessorType() {
			IMAGE_FILE_MACHINE type;
			Utility.CheckHRESULT(((IDebugControl)this.debugClient).GetEffectiveProcessorType(out type));
			switch (type) {
				case IMAGE_FILE_MACHINE.I386:
					return "x86";
				case IMAGE_FILE_MACHINE.AMD64:
					return "Amd64";
				case IMAGE_FILE_MACHINE.ARM:
					return "ARM";
				case IMAGE_FILE_MACHINE.IA64:
					return "Intel Itanium";
				default:
					return "UNKNOWN";
			}
		}

		public string GetOSVersion() {
			var name = new StringBuilder(1024);
			uint length;
			Utility.CheckHRESULT(((IDebugControl4)this.debugClient).GetSystemVersionStringWide(DEBUG_SYSVERSTR.BUILD, name, name.Capacity, out length));
			return name.ToString();
		}

		private IList<SDAppDomain> GetAppDomains() {
			var domains = new List<SDAppDomain>();
			if (context.Runtime != null) {
				foreach (ClrAppDomain clrDomain in this.context.Runtime.AppDomains) {
					var domain = clrDomain.ToSDModel();
					domains.Add(domain);
				}
			}
			return domains;
		}

		private IList<SDClrVersion> GetClrVersions() {
			var versions = new List<SDClrVersion>();
			foreach (ClrInfo info in this.context.Target.ClrVersions) {
				var clrVersion = info.ToSDModel();
				versions.Add(clrVersion);
			}
			return versions;
		}

		private IList<SDModule> GetModuleList() {
			var moduleList = new List<SDModule>();
			foreach (ModuleInfo m in context.Target.EnumerateModules()) {
				// TODO fill systeminfo.modules with information
				var module = m.ToSDModel();
				moduleList.Add(module);
			}
			return moduleList;
		}

		public string GetDumpTime() {
			uint seconds;
			Utility.CheckHRESULT(((IDebugControl2)this.debugClient).GetCurrentTimeDate(out seconds));
			if (seconds != 0) {
				var dateTime = new DateTime(1970, 1, 1);
				dateTime = dateTime.AddSeconds(seconds);
				TimeZone timeZone = TimeZone.CurrentTimeZone;
				TimeZoneInfo info = TimeZoneInfo.Local;
				DateTime localTime = timeZone.ToLocalTime(dateTime);

				return localTime.ToString() + " UTC " + ((info.BaseUtcOffset >= TimeSpan.Zero) ? "+" : "-") + timeZone.GetUtcOffset(localTime);
			}
			return "Could not be obtained.";
		}

		public string GetProcessUpTime() {
			uint seconds;
			Utility.CheckHRESULT(((IDebugSystemObjects2)this.debugClient).GetCurrentProcessUpTime(out seconds));
			if (seconds != 0) {
				TimeSpan time = TimeSpan.FromSeconds(seconds);
				return time.ToString();
			}
			return "Could not be obtained.";
		}

		public string GetSystemUpTime() {
			uint seconds;
			Utility.CheckHRESULT(((IDebugControl2)this.debugClient).GetCurrentSystemUpTime(out seconds));
			if (seconds != 0) {
				TimeSpan time = TimeSpan.FromSeconds(seconds);
				return time.ToString();
			}
			return "Could not be obtained.";
		}

		public uint GetNumberOfProcessors() {
			uint number;
			Utility.CheckHRESULT(((IDebugControl)this.debugClient).GetNumberProcessors(out number));

			return number;
		}

		public void PrintCLRVersions() {
			context.WriteLine("\n--- CLR version list ---");
			foreach (SDClrVersion version in systemInfo.ClrVersions) {
				context.WriteLine("Found CLR Version:" + version.Version);

				// This is the data needed to request the dac from the symbol server:
				SDModule dacInfo = version.DacFile;

				context.WriteLine("Filesize:  {0:X}", dacInfo.FileSize);
				context.WriteLine("Timestamp: {0:X}", dacInfo.TimeStamp);
				context.WriteLine("Dac File:  {0}", dacInfo.FileName);

				context.WriteLine(null);
			}
			context.WriteLine("--- END CLR version list ---");
		}

		public void PrintArchitecture() {
			context.WriteLine("\n--- General System Info ---");
			context.WriteLine("Dump was taken at: {0}", this.systemInfo.DumpTime);
			context.WriteLine("System up-time: {0}", this.systemInfo.SystemUpTime);
			context.WriteLine("OS version: {0}", this.systemInfo.OSVersion);
			context.WriteLine("Process architecture (read from dump): {0}", this.context.Target.Architecture);
			context.WriteLine("Number of logical processing units in target system: {0}", this.systemInfo.NumberOfProcessors);
		}

		/// <summary>
		/// Prints out a list of loaded modules in dump, managed libraries and also native modules
		/// </summary>
		/// <param name="target"></param>
		/// <param name="runtime"></param>
		public void PrintModuleList() {
			context.WriteLine("\n--- Module list ---");
			context.WriteLine("{0,-20:x16} {1,-10} {2,-20} {3} {4}", "Start", "Size/bytes", "Version", "Filename", "Tags");
			foreach (var module in systemInfo.Modules) {
				context.WriteLine("{0,-20:x16} {1,-10:x} {2,-20} {3} {4}",
					module.ImageBase, module.FileSize, module.Version, module.FileName, TagAnalyzer.TagsAsString(string.Empty, module.Tags));
			}
		}

		public void PrintAppDomains() {
			//walking appdomains
			context.WriteLine("\n--- App domains ---");
			foreach (SDAppDomain domain in systemInfo.AppDomains) {
				context.WriteLine("ID:       {0}", domain.Id);
				context.WriteLine("Name:     {0}", domain.Name);
				context.WriteLine("Address:  {0}", domain.Address);
			}
		}

		public void PrintSymbolStateList() {
			context.WriteLine("\n--- Module Symbol state list ---");
			foreach (var module in systemInfo.Modules) {
				PrintSymbolState(module.FileName);
			}
		}

		/// <summary>
		/// Get symbol information for module, see if PDB was loaded or not.
		/// 
		/// -----> CN: does not work if no CLR is loaded
		/// </summary>
		/// <param name="module"></param>
		public void PrintSymbolState(string module) {
			if (!string.IsNullOrEmpty(module)) {
				ClrModule mod = context.Runtime?.Modules.FirstOrDefault(
					m => string.Equals(Path.GetFileName(m.Name), module, StringComparison.InvariantCultureIgnoreCase)); //ignore case

				if (mod != null) {
					ModuleInfo moduleInfo = context.Runtime.DataTarget.EnumerateModules().Single(
						m => string.Equals(m.FileName, mod.FileName, StringComparison.InvariantCultureIgnoreCase));
					context.WriteLine("Module:     {0}", mod.Name);
					context.WriteLine("PDB name:   {0}", moduleInfo.Pdb.FileName);
					context.WriteLine("Debug mode: {0}", mod.DebuggingMode);
				} else {
					// try to find native
					using (DataTarget target = context.CreateTemporaryDbgEngTarget()) {
						ModuleInfo moduleInfo = context.Runtime.DataTarget.EnumerateModules().FirstOrDefault(
							m => string.Equals(Path.GetFileName(m.FileName), module, StringComparison.InvariantCultureIgnoreCase));
						if (moduleInfo == null) {
							return;
						}

						var debugSymbols = (IDebugSymbols3)target.DebuggerInterface;

						uint loaded, unloaded;
						if (0 != debugSymbols.GetNumberModules(out loaded, out unloaded))
							return;

						for (uint moduleIdx = 0; moduleIdx < loaded; ++moduleIdx) {
							var name = new StringBuilder(2048);
							uint nameSize;
							if (0 != debugSymbols.GetModuleNameString(DEBUG_MODNAME.IMAGE, moduleIdx, 0, name, (uint)name.Capacity, out nameSize))
								continue;

							if (!string.Equals(name.ToString(), moduleInfo.FileName, StringComparison.InvariantCultureIgnoreCase))
								continue;

							var modInfo = new DEBUG_MODULE_PARAMETERS[1];
							if (0 != debugSymbols.GetModuleParameters(1, null, moduleIdx, modInfo))
								return;

							name = new StringBuilder(2048);
							debugSymbols.GetModuleNameString(DEBUG_MODNAME.SYMBOL_FILE, moduleIdx, 0, name, (uint)name.Capacity, out nameSize);

							context.WriteLine("Module:     {0}", moduleInfo.FileName);
							context.WriteLine("PDB loaded: {0}", modInfo[0].SymbolType == DEBUG_SYMTYPE.DIA || modInfo[0].SymbolType == DEBUG_SYMTYPE.PDB);
							context.WriteLine("PDB name:   {0}", name.ToString());
						}
					}
				}
			} else {
				context.WriteLine("No module specified.");
			}
		}
	}
}