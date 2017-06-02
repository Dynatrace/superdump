using SuperDump.Models;
using System.Text;

namespace CoreDumpAnalysis {
	public class SharedLibAdapter {

		private readonly IFilesystem filesystem;

		public SharedLibAdapter(IFilesystem filesystem) {
			this.filesystem = filesystem;
		}

		public SDCDModule Adapt(SharedLib lib) {
			SDCDModule module = new SDCDModule();
			module.FilePath = Utf8ArrayToString(lib.Path, 512);
			if(IsBlacklistedPath(module.FilePath)) {
				return null;
			}
			module.FileName = GetFilenameFromPath(module.FilePath);
			module.LocalPath = GetLocalPathFromPath(module.FilePath);
			module.FileSize = (uint)GetFileSizeFromPath(module.LocalPath);
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
			while (end < buffer.Length && buffer[end] != 0 && end < len) {
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

		private string GetLocalPathFromPath(string filepath) {
			if (filesystem.FileExists("." + filepath)) {
				return "." + filepath;
			} else if (filesystem.FileExists(filepath)) {
				return filepath;
			} else {
				return null;
			}
		}

		private long GetFileSizeFromPath(string filepath) {
			if (filepath != null && filesystem.FileExists(filepath)) {
				return filesystem.FileSize(filepath);
			}
			return 0;
		}
	}
}
