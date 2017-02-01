using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;
using System.IO;
using SuperDumpService.Models;
using Hangfire;
using System.IO.Compression;

namespace SuperDumpService.Helpers {
	public static class Utility {
		public static bool ValidateUrl(string path, ref string filename) {
			Uri uri;
			try {
				uri = new Uri(path);
			} catch (Exception ex) {
				Debug.WriteLine(ex.Message);
				return false;
			}

			if (uri.IsFile) {
				return File.Exists(uri.LocalPath);
			}

			var client = new HttpClient() {
				Timeout = new TimeSpan(0, 0, 10)
			};
			try {
				HttpResponseMessage resTask = client.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri)).Result;
				if (string.IsNullOrEmpty(filename)) {
					try {
						filename = resTask.Content.Headers.ContentDisposition.FileName.Replace("\"", "");
					} catch (Exception e) {
						filename = RandomIdGenerator.GetRandomId(1, 15);
						Console.WriteLine($"could not get filename from HEAD request. going with randomly generated '{filename}'. error: {e}");
					}
				} else {
					// go with filename provided
				}
				return true; // resTask.IsSuccessStatusCode; -> urls from our S3 downloads don't accept HEAD requests, but they do accept GET requests. that sucks.
			} catch (TaskCanceledException ex) {
				Console.WriteLine(ex.Message);
				Console.WriteLine("resource at {0} not reachable", uri);
				return false;
			} catch (Exception ex) {
				Console.WriteLine(ex.Message);
				Console.WriteLine("resource at {0} not reachable or other error", uri);
				return false;
			}
		}

		public static bool IsLocalFile(string path) {
			return new Uri(path).IsFile;
		}

		public static List<string> UnzipDumpZip(string zipPath) {
			var urls = new List<string>();
			var zipFileName = Path.GetFileNameWithoutExtension(zipPath);

			using (var zip = System.IO.Compression.ZipFile.OpenRead(zipPath)) {
				foreach (var entry in zip.Entries) {
					if (entry.Name.ToLower().EndsWith(".dmp") || entry.Name.ToLower().EndsWith(".pdb")) {

						string output = Path.Combine(PathHelper.GetUploadsDir(), Path.GetFileNameWithoutExtension(zipFileName), Path.GetFileName(entry.Name));
						string dir = Path.GetDirectoryName(output);
						if (!Directory.Exists(dir)) {
							Directory.CreateDirectory(dir);
						}
						entry.ExtractToFile(output);

						Console.WriteLine("unzipped {0} to {1}", entry.Name, output);
						urls.Add(output);
					}
				}
			}
			return urls;
		}

		public static void ScheduleBundleAnalysis(string workingDir, DumpBundle bundle) {
			// create folder for bundle
			Directory.CreateDirectory(Path.Combine(workingDir, bundle.Id));
			bundle.TimeStamp = DateTime.Now;

			foreach (var item in bundle.DumpItems.Values) {
				//create folders for dumps
				Directory.CreateDirectory(Path.Combine(workingDir, bundle.Id, item.Id));
			}
			// enqueue bundle
			BackgroundJob.Enqueue<IDumpRepository>(repo => repo.AddBundle(JobCancellationToken.Null, bundle));
		}

		public static void RerunAnalysis(string bundleId, string dumpId) {
			var dumpAnalysisItem = new DumpAnalysisItem(bundleId, dumpId) {
				Path = PathHelper.GetDumpfilePath(bundleId, dumpId)
			};
			// enqueue bundle
			BackgroundJob.Enqueue<IDumpRepository>(repo => repo.AddDump(JobCancellationToken.Null, dumpAnalysisItem));
		}

		public static string ConvertWindowsTimeStamp(ulong time) {
			DateTime dateTime = new DateTime(1601, 1, 1);
			dateTime = dateTime.AddSeconds(time / (double)10000000);
			TimeZone timeZone = TimeZone.CurrentTimeZone;
			TimeZoneInfo info = TimeZoneInfo.Local;
			DateTime localTime = timeZone.ToLocalTime(dateTime);

			return localTime.ToString() + " UTC " + ((info.BaseUtcOffset >= TimeSpan.Zero) ? "+" : "-") + timeZone.GetUtcOffset(localTime);
		}

		public static string ConvertWindowsTimeSpan(ulong time) {
			TimeSpan span = TimeSpan.FromSeconds(time / (double)10000000);
			return span.ToString(@"hh\:mm\:ss\:fff");
		}
	}
}
