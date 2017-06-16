﻿using SuperDump.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Thinktecture.IO;
using Thinktecture.IO.Adapters;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;
using SuperDump.Models;
using SuperDump.Analyzer.Linux.Boundary;

namespace SuperDump.Analyzer.Linux.Analysis {
	public class SharedLibAnalyzer {

		private readonly Regex addressRegex = new Regex(@"0x([\da-f]+)\s+0x([\da-f]+)\s+0x([\da-f]+)\s+([^\s]+)", RegexOptions.Compiled);

		private readonly IFilesystem filesystem;

		private readonly IFileInfo coredump;
		private readonly SDResult analysisResult;

		public SharedLibAnalyzer(IFilesystem filesystem, IFileInfo coredump, SDResult analysisResult) {
			this.coredump = coredump ?? throw new ArgumentNullException("Coredump must not be null!");
			this.filesystem = filesystem ?? throw new ArgumentNullException("Filesystem must not be null!");
			this.analysisResult = analysisResult ?? throw new ArgumentNullException("Analysis result must not be null!");
		}

		public async Task AnalyzeAsync() {
			using (var readelf = await ProcessRunner.Run("readelf", new DirectoryInfo(coredump.DirectoryName), "-n", coredump.FullName)) {
				SDCDSystemContext context = analysisResult.SystemContext as SDCDSystemContext ?? new SDCDSystemContext();
				context.Modules = RetrieveLibsFromReadelfOutput(readelf.StdOut).ToList();
			}
		}

		private IEnumerable<SDModule> RetrieveLibsFromReadelfOutput(string readelfOutput) {
			IList<string> lines = readelfOutput.Split(new string[] { System.Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
				.SkipWhile(line => !line.Contains("Page size:")).Skip(2).ToList();
			for (int i = 0; (i + 1) < lines.Count; i += 2) {
				string joined = lines[i] + " " + lines[i + 1];
				Match match = addressRegex.Match(joined);
				if (match.Success) {
					ulong startAddr = Convert.ToUInt64(match.Groups[1].Value, 16);
					ulong endAddr = Convert.ToUInt64(match.Groups[2].Value, 16);
					ulong offset = Convert.ToUInt64(match.Groups[3].Value, 16);
					string path = match.Groups[4].Value;

					if(path == "/dev/zero") {
						continue;
					}

					yield return new SDCDModule() {
						StartAddress = startAddr,
						EndAddress = endAddr,
						Offset = offset,
						FilePath = path,
						ImageBase = 0,
						FileName = GetFilenameFromPath(path),
						LocalPath = GetLocalPathFromPath(path),
						FileSize = (uint)GetFileSizeFromPath(path)
					};
				}
			}
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
			if (filesystem.GetFile("." + filepath).Exists) {
				return "." + filepath;
			} else if (filesystem.GetFile(filepath).Exists) {
				return filepath;
			}
			return null;
		}

		private long GetFileSizeFromPath(string filepath) {
			IFileInfo file = filesystem.GetFile(filepath);
			if (file.Exists) {
				return file.Length;
			}
			return 0;
		}
	}
}
