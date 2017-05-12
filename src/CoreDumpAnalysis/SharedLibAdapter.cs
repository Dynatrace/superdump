using SuperDump.Models;
using SuperDumpModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CoreDumpAnalysis {
	public class SharedLibAdapter {
		public SDCDModule Adapt(SharedLib lib) {
			SDCDModule module = new SDCDModule();
			module.FilePath = Utf8ArrayToString(lib.Path, 512);
			if(IsBlacklistedPath(module.FilePath)) {
				return null;
			}
			module.FileName = GetFilenameFromPath(module.FilePath);
			module.Version = GetVersionFromFilename(module.FileName);
			module.FileSize = (uint)GetFileSizeFromPath(module.FilePath);
			module.ImageBase = 0;
			module.Offset = lib.BindingOffset;
			module.StartAddress = lib.StartAddress;
			module.EndAddress = lib.EndAddress;
			return module;
		}

		private bool IsBlacklistedPath(string filepath) {
			return filepath == "/dev/zero";
		}

		public static string Utf8ArrayToString(byte[] buffer, int len) {
			int end = 0;
			while (buffer[end] != 0 && end < len) {
				end++;
			}
			return Encoding.UTF8.GetString(buffer).Substring(0, end);
		}

		private string GetFilenameFromPath(string filepath) {
			int lastSlash = filepath.LastIndexOf('/');
			if (lastSlash == -1) {
				return filepath;
			} else {
				return filepath.Substring(lastSlash + 1);
			}
		}

		private string GetVersionFromFilename(string filename) {
			int lastDot = filename.LastIndexOf('.');
			int lastDash = filename.LastIndexOf('-');
			if (lastDot == -1 || lastDash == -1 || lastDot <= lastDash) {
				return "";
			}
			return filename.Substring(lastDash + 1, lastDot - lastDash - 1);
		}

		private long GetFileSizeFromPath(string filepath) {
			string lib = "." + filepath;
			// First check if the library can be found in the local directory
			if (File.Exists(lib)) {
				return new FileInfo(lib).Length;
			}
			// Alternatively, take the library from the filesystem
			if (File.Exists(filepath)) {
				return new FileInfo(filepath).Length;
			}
			return 0;
		}
	}
}
