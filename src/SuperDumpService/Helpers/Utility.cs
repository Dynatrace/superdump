using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;
using System.IO;
using SuperDumpService.Models;
using Hangfire;
using System.IO.Compression;
using SuperDumpService.Services;

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

		public static string ConvertWindowsTimeStamp(ulong time) {
			var dateTime = new DateTime(1601, 1, 1);
			dateTime = dateTime.AddSeconds(time / (double)10000000);
			DateTime localTime = dateTime.ToLocalTime();
			return localTime.ToString() + " UTC";
		}

		public static string ConvertWindowsTimeSpan(ulong time) {
			TimeSpan span = TimeSpan.FromSeconds(time / (double)10000000);
			return span.ToString(@"hh\:mm\:ss\:fff");
		}
	}
}
