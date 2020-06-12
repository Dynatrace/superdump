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
using System.Linq;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Threading;

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
				HttpResponseMessage resTask = AsyncHelper.RunSync(() => client.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri)));
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
		internal static IDictionary<string, string> Sanitize(IDictionary<string, string> sourceDict) {
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

		public static void ExtractExe(string command, out string executable, out string arguments) {
			var parts = Regex.Matches(command, @"[\""].+?[\""]|[^ ]+")
				.Cast<Match>()
				.Select(m => m.Value)
				.ToList();
			executable = parts.First();
			arguments = string.Join(" ", parts.Skip(1).ToArray());
		}

		public static string Md5ForFile(FileInfo file) {
			using (var md5 = MD5.Create()) {
				// The .OpenRead method fails if the file is currently open in another process. Let's keep trying for a second.
				for (int i = 0; i < 10; i++) {
					try {
						return ComputeMd5(file, md5);
					} catch (IOException e) {
						Console.WriteLine($"[{i}] Failed to compute MD5 for file {file.FullName} due to a {e.GetType().ToString()}: {e.Message}");
						Thread.Sleep(100);
					}
				}
				return ComputeMd5(file, md5);
			}
		}

		private static string ComputeMd5(FileInfo file, MD5 md5) {
			using (var stream = File.OpenRead(file.FullName)) {
				return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "‌​").ToLower();
			}
		}

		// a value between 0.0 and 1.0
		public static double LevenshteinSimilarity(string source, string target) {
			if (source == target) return 1.0;
			if (string.IsNullOrEmpty(source) ^ string.IsNullOrEmpty(target)) return 0.0;
			int distance = LevenshteinDistance(source, target);
			return 1.0 - ((double)distance / Math.Max(source.Length, target.Length));
		}

		// from https://en.wikibooks.org/wiki/Algorithm_Implementation/Strings/Levenshtein_distance#C#
		// returns the number of modifications that need to be made to "source" in order to become "target"
		public static int LevenshteinDistance(string source, string target) {
			if (string.IsNullOrEmpty(source)) {
				if (string.IsNullOrEmpty(target)) return 0;
				return target.Length;
			}
			if (string.IsNullOrEmpty(target)) return source.Length;

			if (source.Length > target.Length) {
				var temp = target;
				target = source;
				source = temp;
			}

			var m = target.Length;
			var n = source.Length;
			var distance = new int[2, m + 1];
			// Initialize the distance 'matrix'
			for (var j = 1; j <= m; j++) distance[0, j] = j;

			var currentRow = 0;
			for (var i = 1; i <= n; ++i) {
				currentRow = i & 1;
				distance[currentRow, 0] = i;
				var previousRow = currentRow ^ 1;
				for (var j = 1; j <= m; j++) {
					var cost = (target[j - 1] == source[i - 1] ? 0 : 1);
					distance[currentRow, j] = Math.Min(Math.Min(
								distance[previousRow, j] + 1,
								distance[currentRow, j - 1] + 1),
								distance[previousRow, j - 1] + cost);
				}
			}
			return distance[currentRow, m];
		}

		public static async Task BlockUntil(Func<bool> predicate) {
			while (!predicate()) {
				await Task.Delay(500);
			}
		}

		/// <summary>
		/// frames sometimes contain addresses. let's just strip out all non-ascii characters. comparison should still be valid enough.
		/// even modules contain addresses in some cases. also lastevent or exception text.
		/// </summary>
		public static bool EqualsIgnoreNonAlphanumeric(string s1, string s2) {
			if (s1 == null && s2 == null) return true;
			if (s1 == null || s2 == null) return false;
			return StripNonAlphanumeric(s1).Equals(StripNonAlphanumeric(s2), StringComparison.OrdinalIgnoreCase);
		}

		public static string StripNonAlphanumeric(string str) {
			return str == null ? string.Empty : new string(str.ToLower().Where(c => c >= 'a' && c <= 'z').ToArray());
		}

		public static string GetDumpUrl(DumpIdentifier id) {
			return $"/Home/Dump?id={id}";
		}
	}

	public static class StringExtensions {
		public static bool Contains(this string source, string toCheck, StringComparison comp) {
			return source.IndexOf(toCheck, comp) >= 0;
		}
	}

	public static class IEnumerableExtensions {

		// borrowed from https://stackoverflow.com/questions/15414347/how-to-loop-through-ienumerable-in-batches
		public static IEnumerable<IEnumerable<T>> Batch<T>(
		this IEnumerable<T> source, int size) {
			T[] bucket = null;
			var count = 0;

			foreach (var item in source) {
				if (bucket == null)
					bucket = new T[size];

				bucket[count++] = item;

				if (count != size)
					continue;

				yield return bucket.Select(x => x);

				bucket = null;
				count = 0;
			}

			// Return the last bucket with all remaining elements
			if (bucket != null && count > 0) {
				yield return bucket.Take(count);
			}
		}
	}

	public static class AsyncHelper {
		private static readonly TaskFactory _taskFactory = new
			TaskFactory(CancellationToken.None,
						TaskCreationOptions.None,
						TaskContinuationOptions.None,
						TaskScheduler.Default);

		public static TResult RunSync<TResult>(Func<Task<TResult>> func)
			=> _taskFactory
				.StartNew(func)
				.Unwrap()
				.GetAwaiter()
				.GetResult();

		public static void RunSync(Func<Task> func)
			=> _taskFactory
				.StartNew(func)
				.Unwrap()
				.GetAwaiter()
				.GetResult();
	}

	public static class StringExtensionMethods {
		public static int GetStableHashCode(this string str) {
			unchecked {
				int hash1 = 5381;
				int hash2 = hash1;

				for (int i = 0; i < str.Length && str[i] != '\0'; i += 2) {
					hash1 = ((hash1 << 5) + hash1) ^ str[i];
					if (i == str.Length - 1 || str[i + 1] == '\0')
						break;
					hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
				}

				return hash1 + (hash2 * 1566083941);
			}
		}
	}

	public static class DateTimeExtensions {

		/// <summary>
		/// Converts a given DateTime into a Unix timestamp
		/// </summary>
		/// <param name="value">Any DateTime</param>
		/// <returns>The given DateTime in Unix timestamp format</returns>
		public static int ToUnixTimestamp(this DateTime value) {
			return (int)Math.Truncate((value.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds);
		}

		/// <summary>
		/// Gets a Unix timestamp representing the current moment
		/// </summary>
		/// <param name="ignored">Parameter ignored</param>
		/// <returns>Now expressed as a Unix timestamp</returns>
		public static int UnixTimestamp(this DateTime ignored) {
			return (int)Math.Truncate((DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds);
		}
	}
}
