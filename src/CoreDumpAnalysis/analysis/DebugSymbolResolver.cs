using SuperDump.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;

namespace CoreDumpAnalysis {
	public class DebugSymbolResolver {

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
				string hash = CalculateHash(module.LocalPath);
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

		private static string CalculateHash(String path) {
			using (var md5 = MD5.Create()) {
				using (var stream = File.OpenRead(path)) {
					return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
				}
			}
		}

		private bool IsDebugFileAvailable(SDCDModule module, string hash) {
			return File.Exists(DebugFilePath(module.LocalPath, hash));
		}

		private void DownloadDebugSymbols(SDCDModule lib, string hash) {
			Console.WriteLine("Trying to retrieve debug symbols for " + lib.FilePath);
			string url = Constants.DEBUG_SYMBOL_URL_PATTERN.Replace("{hash}", hash).Replace("{file}", DebugFileName(lib.LocalPath));

			HttpClient httpClient = new HttpClient();
			httpClient.GetAsync(url).ContinueWith(
				request => {
					HttpResponseMessage response = request.Result;
					if (request.Result.IsSuccessStatusCode) {
						string filePath = DebugFilePath(lib.LocalPath, hash);
						Directory.CreateDirectory(Path.GetDirectoryName(filePath));
						using (FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None)) {
							response.Content.CopyToAsync(stream);
						}
						lib.DebugSymbolPath = Path.GetFullPath(filePath);
						Console.WriteLine("Successfully downloaded debug symbols for " + lib.FilePath);
					}
				}).Wait();
		}

		private static string DebugFilePath(String path) {
			return DebugFilePath(path, CalculateHash(path));
		}

		private static string DebugFilePath(String path, string hash) {
			return Constants.DEBUG_SYMBOL_PATH + hash + "/" + DebugFileName(path);
		}

		public static string DebugFileName(String path) {
			int lastDot = path.LastIndexOf('.');
			int lastSlash = path.LastIndexOf('/');
			if(lastDot == -1 || lastDot <= lastSlash) {
				return path.Substring(Math.Max(lastDot, lastSlash) + 1) + ".dbg";
			}
			return path.Substring(lastSlash + 1, lastDot - lastSlash - 1) + ".dbg";
		}
	}
}
