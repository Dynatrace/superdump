using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CoreDumpAnalysis {
	class FilesystemHelper {
		public static String GetParentDirectory(String dir) {
			int idx = dir.LastIndexOfAny(new char[] { '/', '\\' });
			if (idx >= 0) {
				return dir.Substring(0, idx) + "/";
			}
			return "./";
		}

		public static List<String> FilesInDirectory(String directory) {
			List<String> files = new List<string>();
			FilesInDirectoryRec(directory, files);
			return files;
		}

		private static void FilesInDirectoryRec(String directory, List<String> current) {
			foreach (string f in Directory.GetFiles(directory)) {
				current.Add(f);
			}
			try {
				foreach (string d in Directory.GetDirectories(directory)) {
					current.AddRange(FilesInDirectory(d));
				}
			} catch (System.Exception excpt) {
				Console.WriteLine(excpt.Message);
			}
		}
	}
}
