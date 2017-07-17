using SuperDump.Analyzer.Linux.Boundary;
using SuperDump.Common;
using SuperDump.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDump.Analyzer.Linux.Analysis {
	public class DebugSymbolResolver {

		private readonly IFilesystem filesystem;
		private readonly IHttpRequestHandler requestHandler;
		private readonly IProcessHandler processHandler;

		public DebugSymbolResolver(IFilesystem filesystem, IHttpRequestHandler requestHandler, IProcessHandler processHandler) {
			this.filesystem = filesystem ?? throw new ArgumentNullException("Filesystem Helper must not be null!");
			this.requestHandler = requestHandler ?? throw new ArgumentNullException("RequestHandler must not be null!");
			this.processHandler = processHandler ?? throw new ArgumentNullException("ProcessHandler must not be null!");
		}

		public void Resolve(IList<SDModule> libs) {
			DownloadDebugSymbolsAsync(WithoutDuplicates(libs)).Wait();
		}

		private IEnumerable<SDModule> WithoutDuplicates(IList<SDModule> libs) {
			return libs
				.GroupBy(l => l.FilePath)
				.Select(g => g.First());
		}

		private async Task DownloadDebugSymbolsAsync(IEnumerable<SDModule> modules) {
			foreach (SDModule module in modules) {
				if (module is SDCDModule) {
					await DownloadDebugSymbolForModuleAsync((SDCDModule)module);
				} else {
					throw new InvalidCastException("Provided module is not a coredump module!");
				}
			}
		}

		private async Task DownloadDebugSymbolForModuleAsync(SDCDModule module) {
			if (module.LocalPath != null && IsDynatraceModule(module)) {
				string hash = filesystem.Md5FromFile(module.LocalPath);
				if (IsDebugFileAvailable(module, hash)) {
					module.DebugSymbolPath = Path.GetFullPath(DebugFilePath(module.LocalPath, hash));
				} else {
					await DownloadDebugSymbolsAsync(module, hash);
				}

				await UnstripLibrary(module, hash);
			}
		}

		/// <summary>
		/// Overrides the original *.so with the unstripped binary
		/// </summary>
		private async Task UnstripLibrary(SDCDModule module, string hash) {
			if (IsDebugFileAvailable(module, hash)) {
				filesystem.Move(module.LocalPath, module.LocalPath + ".old");
				await processHandler.ExecuteProcessAndGetOutputAsync("eu-unstrip",
					$"-o {module.LocalPath} {module.LocalPath}.old {DebugFilePath(module.LocalPath, hash)}");
				filesystem.Delete(module.LocalPath + ".old");
			}
		}

		private bool IsDynatraceModule(SDCDModule module) {
			if (module.FileName.Contains("libruxit") || module.FileName.Contains("liboneagent")) {
				return true;
			}
			if (module.FilePath.Contains("/lib/ruxit") || module.FilePath.Contains("/lib64/ruxit") ||
				module.FilePath.Contains("/lib/oneagent") || module.FilePath.Contains("/lib64/oneagent")) {
				return true;
			}
			return false;
		}

		private bool IsDebugFileAvailable(SDCDModule module, string hash) {
			return filesystem.GetFile(DebugFilePath(module.LocalPath, hash)).Exists;
		}

		private async Task DownloadDebugSymbolsAsync(SDCDModule lib, string hash) {
			Console.WriteLine($"Trying to retrieve debug symbols for {lib.FilePath}");
			string url = Constants.DEBUG_SYMBOL_URL_PATTERN.Replace("{hash}", hash).Replace("{file}", DebugFileName(lib.LocalPath));

			string localDebugFile = DebugFilePath(lib.LocalPath, hash);
			try {
				if (await requestHandler.DownloadFromUrlAsync(url, localDebugFile)) {
					Console.WriteLine($"Successfully downloaded debug symbols for {lib.FilePath}. Stored at {localDebugFile}");
					lib.DebugSymbolPath = Path.GetFullPath(localDebugFile);
				} else {
					Console.WriteLine($"Failed to download debug symbols for {lib.FilePath}. URL: {url}");
				}
			} catch (Exception e) {
				Console.WriteLine($"Failed to download debug symbol: {e.Message}");
			}
		}

		private string DebugFilePath(string path, string hash) {
			return Path.Combine(Constants.DEBUG_SYMBOL_PATH, hash, DebugFileName(path));
		}

		public static string DebugFileName(string path) {
			return $"{Path.GetFileNameWithoutExtension(path)}.dbg";
		}
	}
}
