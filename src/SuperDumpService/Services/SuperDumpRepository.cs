using System;
using System.Collections.Generic;
using System.Linq;
using SuperDump.Models;
using System.IO;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using System.Net;
using System.Diagnostics;
using Hangfire;
using SuperDumpService.Helpers;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Threading.Tasks;
using SuperDumpService.Models;

namespace SuperDumpService.Services {
	public class SuperDumpRepository : ISuperDumpRepository {
		// collection that holds information about all dump bundles and their dumps
		private static ConcurrentDictionary<string, DumpBundle> dumpBundles = new ConcurrentDictionary<string, DumpBundle>();
		private static ConcurrentDictionary<string, DumpAnalysisItem> dumpItems = new ConcurrentDictionary<string, DumpAnalysisItem>();

		private IOptions<SuperDumpSettings> settings;
		private SymStoreHelper symStoreHelper;

		public SuperDumpRepository(IOptions<SuperDumpSettings> settings) {
			this.settings = settings;
			this.symStoreHelper = new SymStoreHelper(settings.Value.LocalSymbolCache, settings.Value.SymStoreExex64, settings.Value.SymStoreExex86);
			PathHelper.PrepareDirectories();
			PopulateFromDisk();
		}

		private static void PopulateFromDisk() {
			// look for all dumps and init dictionary
			try {
				foreach (var dir in Directory.EnumerateDirectories(PathHelper.GetWorkingDir())) {
					var bundleId = new DirectoryInfo(dir).Name;

					// init bundle
					dumpBundles[bundleId] = new DumpBundle(bundleId) { DownloadCompleted = true };

					// enumerate directories in bundle
					foreach (var dumpDir in Directory.EnumerateDirectories(dir)) {
						var dumpId = new DirectoryInfo(dumpDir).Name;

						string dump = Directory.EnumerateFiles(dumpDir)
												.FirstOrDefault(file => file.ToLower().EndsWith(dumpId + ".dmp"));
						string jsonResult = Directory.EnumerateFiles(dumpDir).FirstOrDefault(file => file.ToLower().EndsWith(dumpId + ".json"));

						bool dumpIsThere = !string.IsNullOrEmpty(dump);
						bool resultIsThere = !string.IsNullOrEmpty(jsonResult);

						if (resultIsThere) {
							// load result and get information
							string json;
							using (StreamReader reader = new StreamReader(File.OpenRead(jsonResult))) {
								json = reader.ReadToEnd();
							}
							SDResult res = JsonConvert.DeserializeObject<SDResult>(json);
							// init item
							var item = new DumpAnalysisItem(bundleId, dumpId) {
								IsAnalysisComplete = true,
								JiraIssue = res.AnalysisInfo.JiraIssue,
								FriendlyName = res.AnalysisInfo.FriendlyName,
								ResultPath = jsonResult,
								Path = res.AnalysisInfo.Path,
								TimeStamp = res.AnalysisInfo.ServerTimeStamp
							};
							// get info if anomalies were detected
							if (res.ExceptionRecord.Count > 0 || res.DeadlockInformation.Count > 0 || (res.LastEvent != null && res.LastEvent.Type == "EXCEPTION")) {
								item.AnomaliesDetected = true;
							}
							//add to list for bundle
							dumpBundles[bundleId].DumpItems[dumpId] = item;
							dumpItems[dumpId] = dumpBundles[bundleId].DumpItems[dumpId]; // store this reference also in dumpitems

							dumpBundles[bundleId].JiraIssue = res.AnalysisInfo.JiraIssue;
							dumpBundles[bundleId].FriendlyName = res.AnalysisInfo.FriendlyName;
							dumpBundles[bundleId].Path = res.AnalysisInfo.Path;
							dumpBundles[bundleId].TimeStamp = res.AnalysisInfo.ServerTimeStamp;
						}
					}
					dumpBundles[bundleId].IsAnalysisComplete = true;
				}
			} catch (UnauthorizedAccessException ex) {
				Debug.WriteLine(ex.Message);
			} catch (PathTooLongException ex) {
				Debug.WriteLine(ex.Message);
			}
		}

		public async Task AddBundle(IJobCancellationToken token, DumpBundle bundle) {
			try {
				dumpBundles[bundle.Id] = bundle;
				Uri uri = new Uri(bundle.Url); // should not throw an exception due to validation before!
											   //check if local or not
				if (!Utility.IsLocalFile(bundle.Url)) {
					// download
					dumpBundles[bundle.Id].Path = Path.Combine(FindUniqueFilename(PathHelper.GetBundleDownloadPath(bundle.UrlFilename)));
					using (var client = new HttpClient()) {
						using (var download = await client.GetAsync(uri)) {
							using (var stream = await download.Content.ReadAsStreamAsync()) {
								using (var outfile = File.OpenWrite(dumpBundles[bundle.Id].Path)) {
									await stream.CopyToAsync(outfile);
								}
							}
						}
					}
				} else {
					dumpBundles[bundle.Id].Path = bundle.Url;
				}
				token.ThrowIfCancellationRequested();

				// file is now at uploads/filename, now start to process it
				// copy to dumps folder

				if (dumpBundles[bundle.Id].Path.ToLower().EndsWith(".zip")) {
					var zipItems = Utility.UnzipDumpZip(bundle.Path);

					if (zipItems.Count <= 0) {
						DeleteBundle(bundle.Id);
						throw new NoDumpInZipException("No dump files in zip found!");
					}

					// put symbols in symbolcache first
					foreach (var path in zipItems.Where(path => CanBePutInSymbolStore(path))) {
						symStoreHelper.AddToSymStore(path);
					}

					// iterate dumps and schedule analysis
					foreach (var path in zipItems.Where(path => path.EndsWith(".dmp", StringComparison.OrdinalIgnoreCase))) {
						// create new item for each dump in zip and add to bundle
						DumpAnalysisItem newItem = new DumpAnalysisItem { Id = CreateUniqueDumpId(), BundleId = bundle.Id, Path = path, JiraIssue = bundle.JiraIssue, FriendlyName = bundle.FriendlyName };
						newItem.TimeStamp = DateTime.Now;
						bundle.DumpItems[newItem.Id] = newItem;
						dumpItems[newItem.Id] = bundle.DumpItems[newItem.Id];
						if (!Directory.Exists(PathHelper.GetDumpfilePath(bundle.Id, newItem.Id))) {
							Directory.CreateDirectory(PathHelper.GetDumpDirectory(bundle.Id, newItem.Id));
						}
						// copy file to dumps directory
						//File.Copy(path, PathHelper.GetDumpfilePath(bundle.Id, newItem.Id));

						// start analysis
						BackgroundJob.Enqueue<ISuperDumpRepository>(repo => repo.AddDump(JobCancellationToken.Null, newItem));
					}
					bundle.DownloadCompleted = true;
				} else if (CanBePutInSymbolStore(dumpBundles[bundle.Id].Path)) {
					// automatically store into symbol cache
					symStoreHelper.AddToSymStore(dumpBundles[bundle.Id].Path);
				} else {
					DumpAnalysisItem newItem = new DumpAnalysisItem { Id = CreateUniqueDumpId(), BundleId = bundle.Id, Path = bundle.Path, JiraIssue = bundle.JiraIssue, FriendlyName = bundle.FriendlyName };
					newItem.TimeStamp = DateTime.Now;
					bundle.DumpItems[newItem.Id] = newItem;
					dumpItems[newItem.Id] = bundle.DumpItems[newItem.Id];
					if (!Directory.Exists(PathHelper.GetDumpDirectory(bundle.Id, newItem.Id))) {
						Directory.CreateDirectory(PathHelper.GetDumpDirectory(bundle.Id, newItem.Id));
					}
					// copy file to dumps directory
					//File.Copy(newItem.Path, PathHelper.GetDumpfilePath(bundle.Id, newItem.Id));
					BackgroundJob.Enqueue<ISuperDumpRepository>(repo => repo.AddDump(JobCancellationToken.Null, bundle.DumpItems[newItem.Id]));
					bundle.DownloadCompleted = true;
				}
			} catch (NoDumpInZipException) {
				DeleteBundle(bundle.Id);
				throw;
			} catch (OperationCanceledException e) {
				Console.WriteLine(e.Message);
				dumpBundles[bundle.Id].AnalysisError += e.Message;
				dumpBundles[bundle.Id].HasAnalysisFailed = true;
			} catch (Exception e) {
				dumpBundles[bundle.Id].AnalysisError += e.Message;
				dumpBundles[bundle.Id].HasAnalysisFailed = true;
				throw;
			}
		}

		private static string FindUniqueFilename(string fullpath) {
			int i = 1;
			string directory = Path.GetDirectoryName(fullpath);
			string filenameWoExtension = Path.GetFileNameWithoutExtension(fullpath);
			string filename = Path.GetFileName(fullpath);
			string extension = Path.GetExtension(filename);

			while (File.Exists(fullpath)) {
				fullpath = Path.Combine(directory, $"{filenameWoExtension}_{i}{extension}");
				i++;
			}
			return fullpath;
		}

		private static bool CanBePutInSymbolStore(string path) {
			return path.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
		}

		public void AddDump(IJobCancellationToken token, DumpAnalysisItem item) {
			try {
				//todo: throw away that WebClient thing, at this point the file should already be downloaded
				if (item.Path != PathHelper.GetDumpfilePath(item.BundleId, item.Id)) { // in case its a re-trigger, no need to copy
					File.Copy(item.Path, PathHelper.GetDumpfilePath(item.BundleId, item.Id));
				}
				token.ThrowIfCancellationRequested();
				try {
					string dumpselector = PathHelper.GetDumpSelectorPath();
					Process p = new Process();
					p.StartInfo.FileName = dumpselector;
					p.StartInfo.Arguments = PathHelper.GetDumpfilePath(item.BundleId, item.Id); // dump file location
					Console.WriteLine(p.StartInfo.Arguments);
					p.StartInfo.RedirectStandardOutput = true;
					p.StartInfo.RedirectStandardError = true;
					p.StartInfo.UseShellExecute = false;
					p.StartInfo.CreateNoWindow = true;

					Console.WriteLine($"launching '{p.StartInfo.FileName}' '{p.StartInfo.Arguments}'");
					token.ThrowIfCancellationRequested();
					p.Start();
					p.PriorityClass = ProcessPriorityClass.BelowNormal;
					string stdout = p.StandardOutput.ReadToEnd(); // important to do ReadToEnd before WaitForExit to avoid deadlock
					string stderr = p.StandardError.ReadToEnd();
					p.WaitForExit();
					string selectorLog = $"SuperDumpSelector exited with error code {p.ExitCode}" +
						$"{Environment.NewLine}{Environment.NewLine}stdout:{Environment.NewLine}{stdout}" +
						$"{Environment.NewLine}{Environment.NewLine}stderr:{Environment.NewLine}{stderr}";
					Console.WriteLine(selectorLog);
					File.WriteAllText(Path.Combine(PathHelper.GetDumpDirectory(item.BundleId, item.Id), "superdumpselector.log"), selectorLog);
					if (p.ExitCode != 0) {
						throw new Exception(selectorLog);
					} else {
						AddResult(item.BundleId, item.Id, PathHelper.GetJsonPath(item.BundleId, item.Id));
					}
					token.ThrowIfCancellationRequested();
				} catch (OperationCanceledException e) {
					Console.WriteLine(e.Message);
				}
			} catch (OperationCanceledException e) {
				Console.WriteLine(e.Message);
				item.HasAnalysisFailed = true;
				item.AnalysisError += e.Message;
			} catch (Exception e) {
				Console.WriteLine(e.Message);
				item.HasAnalysisFailed = true;
				item.AnalysisError = e.Message;
				// this would be a good place to write that error to some overview.json file
				throw; // throw let the hangfire job fail
			}
		}

		public void AddResult(string bundleId, string id, string resultPath) {
			if (dumpBundles.ContainsKey(bundleId) && dumpBundles[bundleId].DumpItems.ContainsKey(id)) {
				dumpBundles[bundleId].DumpItems[id].ResultPath = resultPath;
				dumpBundles[bundleId].DumpItems[id].IsAnalysisComplete = true;

				// load result and add own information
				string json;
				using (StreamReader reader = new StreamReader(File.OpenRead(dumpBundles[bundleId].DumpItems[id].ResultPath))) {
					json = reader.ReadToEnd();
				}
				// deserialize and store own information
				SDResult res = JsonConvert.DeserializeObject<SDResult>(json);
				res.AnalysisInfo.FileName = dumpBundles[bundleId].DumpItems[id].FileName;
				res.AnalysisInfo.Path = dumpBundles[bundleId].DumpItems[id].Path;
				res.AnalysisInfo.JiraIssue = dumpBundles[bundleId].DumpItems[id].JiraIssue;
				res.AnalysisInfo.FriendlyName = dumpBundles[bundleId].DumpItems[id].FriendlyName;
				res.AnalysisInfo.ServerTimeStamp = DateTime.Now;

				// get info if anomalies were detected
				if (res.ExceptionRecord.Count > 0 || res.DeadlockInformation.Count > 0) {
					dumpBundles[bundleId].DumpItems[id].AnomaliesDetected = true;
				}

				res.WriteResultToJSONFile(resultPath);

				// check if all items have been completed
				bool isComplete = true;
				foreach (var item in dumpBundles[bundleId].DumpItems.Values) {
					if (!item.IsAnalysisComplete) {
						isComplete = false;
						break;
					}
				}
				dumpBundles[bundleId].IsAnalysisComplete = isComplete;
			}
		}

		public IEnumerable<DumpBundle> GetAll() {
			return dumpBundles.Values;
		}

		public SDResult GetResult(string bundleId, string id) {
			if (ContainsDump(bundleId, id)
				&& dumpBundles[bundleId].DumpItems[id].ResultPath != null
				&& dumpBundles[bundleId].DumpItems[id].IsAnalysisComplete) {
				string json;
				using (StreamReader reader = new StreamReader(File.OpenRead(dumpBundles[bundleId].DumpItems[id].ResultPath))) {
					json = reader.ReadToEnd();
				}
				return JsonConvert.DeserializeObject<SDResult>(json);
			}
			return null;
		}

		public void DeleteDump(string bundleId, string id) {
			if (id != null) {
				DumpBundle value;
				dumpBundles.TryRemove(id, out value);

				if (Directory.Exists(PathHelper.GetDumpDirectory(bundleId, id))) {
					Directory.Delete(PathHelper.GetDumpDirectory(bundleId, id), true);
				}
			}
		}

		public void DeleteBundle(string bundleId) {
			if (bundleId != null) {
				DumpBundle value;
				dumpBundles.TryRemove(bundleId, out value);

				if (Directory.Exists(PathHelper.GetBundleDirectory(bundleId))) {
					Directory.Delete(PathHelper.GetBundleDirectory(bundleId), true);
				}
			}
		}

		public bool ContainsDump(string bundleId, string id) {
			if (dumpBundles.ContainsKey(bundleId)) {
				return dumpBundles[bundleId].DumpItems.ContainsKey(id);
			}
			return dumpBundles.ContainsKey(bundleId);
		}

		public bool ContainsBundle(string bundleId) {
			if (bundleId != null) {
				return dumpBundles.ContainsKey(bundleId);
			} else {
				return false;
			}
		}

		public DumpBundle GetBundle(string bundleId) {
			if (dumpBundles.ContainsKey(bundleId)) {
				return dumpBundles[bundleId];
			} else {
				return null;
			}
		}

		public DumpAnalysisItem GetDump(string bundleId, string id) {
			if (!string.IsNullOrEmpty(bundleId) && !string.IsNullOrEmpty(id)) {
				if (dumpBundles.ContainsKey(bundleId) && dumpBundles[bundleId].DumpItems.ContainsKey(id)) {
					var dumpAnalysisItem = dumpBundles[bundleId].DumpItems[id];
					if (dumpAnalysisItem != null) {
						dumpAnalysisItem.Files = GetReportFileNames(bundleId, id);
						return dumpAnalysisItem;
					}
					return null;
				} else {
					return null;
				}
			} else {
				return null;
			}
		}

		public DumpAnalysisItem GetDump(string dumpId) {
			if (!string.IsNullOrEmpty(dumpId)) {
				if (dumpItems.ContainsKey(dumpId)) {
					var dumpAnalysisItem = dumpItems[dumpId];
					if (dumpAnalysisItem != null) {
						dumpAnalysisItem.Files = GetReportFileNames(dumpAnalysisItem.BundleId, dumpId);
						return dumpAnalysisItem;
					}
					return null;
				} else {
					return null;
				}
			} else {
				return null;
			}
		}

		public IEnumerable<string> GetReportFileNames(string bundleId, string id) {
			foreach (var file in Directory.EnumerateFiles(PathHelper.GetDumpDirectory(bundleId, id))) {
				yield return new FileInfo(file).Name;
			}
		}

		public FileInfo GetReportFile(string bundleId, string id, string filename) {
			var f = new FileInfo(Path.Combine(PathHelper.GetDumpDirectory(bundleId, id), filename));
			if (f.Exists) {
				return f;
			} else {
				return null;
			}
		}

		public bool ContainsDump(string dumpId) {
			if (dumpId != null) {
				return dumpItems.ContainsKey(dumpId);
			} else {
				return false;
			}
		}

		public string CreateUniqueBundleId() {
			// create bundleId and make sure it does not exist yet.
			while (true) {
				string bundleId = RandomIdGenerator.GetRandomId();
				if (!ContainsBundle(bundleId)) {
					// does not exist yet. yay.
					return bundleId;
				}
			}
		}

		public string CreateUniqueDumpId() {
			// create dumpId and make sure it does not exist yet.
			while (true) {
				string dumpId = RandomIdGenerator.GetRandomId();
				if (!ContainsDump(dumpId)) {
					// does not exist yet. yay.
					return dumpId;
				}
			}
		}

		public void WipeAllExceptDump(string bundleId, string dumpId) {
			var dumpdir = PathHelper.GetDumpDirectory(bundleId, dumpId);
			var dumpfile = PathHelper.GetDumpfilePath(bundleId, dumpId);
			foreach (var file in Directory.EnumerateFiles(dumpdir)) {
				if (file != dumpfile) {
					File.Delete(file);
				}
			}
		}
	}
}