using SuperDump.Analyzer.Linux.boundary;
using SuperDump.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace SuperDump.Analyzer.Linux {
	public class DebugSymbolResolver {

		private readonly IFilesystem filesystem;
		private readonly IHttpRequestHandler requestHandler;

		public DebugSymbolResolver(IFilesystem filesystem, IHttpRequestHandler requestHandler) {
			this.filesystem = filesystem ?? throw new ArgumentNullException("Filesystem Helper must not be null!");
			this.requestHandler = requestHandler ?? throw new ArgumentNullException("RequestHandler must not be null!");
		}

		public void Resolve(IList<SDModule> libs) {
			DownloadDebugSymbols(WithoutDuplicates(libs));
		}

		private List<SDModule> WithoutDuplicates(IList<SDModule> libs) {
			var distincts = new List<SDModule>();
			foreach (SDCDModule lib in libs) {
				if (!distincts.Exists(l => l.FilePath == lib.FilePath)) {
					distincts.Add(lib);
				}
			}
			return distincts;
		}

		private void DownloadDebugSymbols(IList<SDModule> modules) {
			foreach(SDModule module in modules) {
				if (module is SDCDModule) {
					DownloadDebugSymbolForModule((SDCDModule)module);
				} else {
					throw new InvalidCastException("Provided module is not a coredump module!");
				}
			}
		}

		private void DownloadDebugSymbolForModule(SDCDModule module) {
			if (module.LocalPath != null && DebugSymbolsRelevant(module)) {
				string hash = filesystem.Md5FromFile(module.LocalPath);
				if (IsDebugFileAvailable(module, hash)) {
					module.DebugSymbolPath = Path.GetFullPath(DebugFilePath(module.LocalPath, hash));
				} else {
					DownloadDebugSymbols(module, hash);
				}
			}
		}

		private bool DebugSymbolsRelevant(SDCDModule module) {
			if(module.FileName.Contains("libruxit") || module.FileName.Contains("liboneagent")) {
				return true;
			}
			if(module.FilePath.Contains("/lib/ruxit") || module.FilePath.Contains("/lib64/ruxit") || 
				module.FilePath.Contains("/lib/oneagent") || module.FilePath.Contains("/lib64/oneagent")) {
				return true;
			}
			return false;
		}
		
		private bool IsDebugFileAvailable(SDCDModule module, string hash) {
			return filesystem.FileExists(DebugFilePath(module.LocalPath, hash));
		}

		private void DownloadDebugSymbols(SDCDModule lib, string hash) {
			Console.WriteLine("Trying to retrieve debug symbols for " + lib.FilePath);
			string url = Constants.DEBUG_SYMBOL_URL_PATTERN.Replace("{hash}", hash).Replace("{file}", DebugFileName(lib.LocalPath));

			string localDebugFile = DebugFilePath(lib.LocalPath, hash);
			try {
				if (requestHandler.DownloadFromUrl(url, localDebugFile)) {
					Console.WriteLine("Successfully downloaded debug symbols for " + lib.FilePath + ". Stored at " + localDebugFile);
					lib.DebugSymbolPath = Path.GetFullPath(localDebugFile);
				} else {
					Console.WriteLine("Failed to download debug symbols for " + lib.FilePath + ". URL: " + url);
				}
			} catch(Exception e) {
				Console.WriteLine("Failed to download debug symbol: " + e.Message);
			}
		}

		private string DebugFilePath(string path, string hash) {
			return Constants.DEBUG_SYMBOL_PATH + hash + "/" + DebugFileName(path);
		}

		public static string DebugFileName(string path) {
			int lastDot = path.LastIndexOf('.');
			int lastSlash = path.LastIndexOf('/');
			if(lastDot == -1 || lastDot <= lastSlash) {
				return path.Substring(Math.Max(lastDot, lastSlash) + 1) + ".dbg";
			}
			return path.Substring(lastSlash + 1, lastDot - lastSlash - 1) + ".dbg";
		}
	}
}
