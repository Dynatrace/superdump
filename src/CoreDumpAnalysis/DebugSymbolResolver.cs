using SuperDump.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace CoreDumpAnalysis {
	public class DebugSymbolResolver {

		public DebugSymbolResolver() {
		}

		public void Resolve(IList<SDModule> libs) {
			List<SDModule> distincts = WithoutDuplicates(libs);
			var tasks = new List<Task>();
			distincts.ForEach(module => tasks.Add(DownloadDebugSymbols(module)));
			Task.WaitAll(tasks.ToArray());
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

		private async Task DownloadDebugSymbols(SDModule lib) {
			if(lib is SDCDModule) {
				await DownloadDebugSymbols((SDCDModule)lib);
			} else {
				throw new InvalidOperationException("Module list must contain coredump modules only!");
			}
		}

		private async Task DownloadDebugSymbols(SDCDModule lib) {
			if(lib.LocalPath == null) {
				Console.WriteLine("Shared library " + lib.FilePath + " not found on local filesystem. Skipping debug symbol resolution.");
				return;
			}

			string hash = CaculateHash(lib.LocalPath);
			string url = Constants.DEBUG_SYMBOL_URL_PATTERN.Replace("{hash}", hash).Replace("{file}", DebugFileName(lib.LocalPath));

			HttpClient httpClient = new HttpClient();
			await httpClient.GetAsync(url).ContinueWith(
				request => {
					HttpResponseMessage response = request.Result;
					if (request.Result.IsSuccessStatusCode) {
						FileStream stream = new FileStream(DebugFilePath(lib.LocalPath), FileMode.Create, FileAccess.Write, FileShare.None);
						response.Content.CopyToAsync(stream);
						Console.WriteLine("Successfully downloaded debug symbols for " + lib.FilePath);
					}
				});
		}

		private string CaculateHash(String path) {
			using (var md5 = MD5.Create()) {
				using (var stream = File.OpenRead(path)) {
					return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
				}
			}
		}

		private string DebugFileName(String path) {
			int lastDot = path.LastIndexOf('.');
			int lastSlash = path.LastIndexOf('/');
			if(lastDot == -1 || lastDot <= lastSlash) {
				return path.Substring(Math.Max(lastDot, lastSlash) + 1) + ".dbg";
			}
			return path.Substring(lastSlash + 1, lastDot - lastSlash - 1) + ".dbg";
		}

		private string DebugFilePath(String path) {
			int lastDot = path.LastIndexOf('.');
			if (lastDot <= 0) {
				return path + ".dbg";
			}
			return path.Substring(0, lastDot) + ".dbg";
		}
	}
}
