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
using System.Reflection;
using System.ComponentModel;

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

		internal static string MakeRelativePath(string folder, FileInfo file) {
			Uri pathUri = new Uri(file.FullName);
			// Folders must end in a slash
			if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString())) {
				folder += Path.DirectorySeparatorChar;
			}
			Uri folderUri = new Uri(folder);
			return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
		}

		/// <summary>
		/// make key and values safe against XSS
		/// </summary>
		internal static IDictionary<string, string> Sanitize(Dictionary<string, string> sourceDict) {
			var dict = new Dictionary<string, string>();
			foreach (var entry in sourceDict) {
				dict[Sanitize(entry.Key)] = Sanitize(entry.Value);
			}
			return dict;
		}

		private static string Sanitize(string value) {
			return System.Net.WebUtility.HtmlEncode(value);
		}

		private static readonly string[] SizeSuffixes =
						   { "b", "kb", "mb", "gb", "tb", "pb", "eb", "zb", "yb" };
		public static string FormattedBytes(long value, int decimalPlaces = 1) {
			if (value < 0) { return "-" + FormattedBytes(-value); }
			if (value == 0) { return "0.0 bytes"; }

			// mag is 0 for bytes, 1 for KB, 2, for MB, etc.
			int mag = (int)Math.Log(value, 1024);

			// 1L << (mag * 10) == 2 ^ (10 * mag) 
			// [i.e. the number of bytes in the unit corresponding to mag]
			decimal adjustedSize = (decimal)value / (1L << (mag * 10));

			// make adjustment when the value is large enough that
			// it would round up to 1000 or more
			if (Math.Round(adjustedSize, decimalPlaces) >= 1000) {
				mag += 1;
				adjustedSize /= 1024;
			}

			return string.Format("{0:n" + decimalPlaces + "}{1}",
				adjustedSize,
				SizeSuffixes[mag]);
		}

		public static string GetEnumDescription(Enum value) {
			FieldInfo fi = value.GetType().GetField(value.ToString());

			DescriptionAttribute[] attributes =
				(DescriptionAttribute[])fi.GetCustomAttributes(
				typeof(DescriptionAttribute),
				false);

			if (attributes != null &&
				attributes.Length > 0) {
				return attributes[0].Description;
			} else {
				return value.ToString();
			}
		}

		public static async Task<FileInfo> CopyFile(FileInfo sourceFile, FileInfo destFile) {
			using (Stream source = sourceFile.OpenRead()) {
				using (Stream destination = destFile.Create()) {
					await source.CopyToAsync(destination);
				}
			}
			return destFile;
		}

		public static bool IsSubdirectoryOf(DirectoryInfo parentDir, DirectoryInfo subDir) {
			var di1 = parentDir;
			var di2 = subDir;
			while (di2.Parent != null) {
				if (DirectoryEquals(di2, di1)) return true;
				di2 = di2.Parent;
			}
			return false;
		}

		private static bool DirectoryEquals(DirectoryInfo path1, DirectoryInfo path2) {
			return DirectoryEquals(path1.FullName, path2.FullName);
		}

		private static bool DirectoryEquals(string path1, string path2) {
			return string.Equals(
					Path.GetFullPath(path1).TrimEnd('\\'),
					Path.GetFullPath(path2).TrimEnd('\\'),
					StringComparison.OrdinalIgnoreCase);
		}
	}
}
