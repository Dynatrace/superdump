using SuperDump.Analyzer.Linux.Boundary;
using SuperDump.Common;
using SuperDump.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thinktecture.IO;

namespace SuperDump.Analyzer.Linux.Analysis {
	public class SourceFileProvider {
		private readonly IFilesystem filesystem;
		private readonly IHttpRequestHandler requestHandler;
		private readonly SDResult result;

		public SourceFileProvider(SDResult result, IFilesystem filesystem, IHttpRequestHandler requestHandler) {
			this.result = result;
			this.filesystem = filesystem ?? throw new NullReferenceException("Filesystem must not be null!");
			this.requestHandler = requestHandler ?? throw new NullReferenceException("Request Handler must not be null!");
		}

		public void ProvideSourceFiles(string directory) {
			IList<Task> tasks = new List<Task>();
			IList<string> files = new List<string>();
			if (result.ThreadInformation == null) {
				return;
			}
			if (string.IsNullOrEmpty(Configuration.SOURCE_REPO_URL)) {
				return;
			}
			foreach (var thread in result.ThreadInformation) {
				if (thread.Value.StackTrace == null) {
					continue;
				}
				foreach (var stackframe in thread.Value.StackTrace) {
					string file = stackframe.SourceInfo?.File;
					if (file == null) {
						// no source info
						continue;
					}
					string repoPath = DynatraceSourceLink.GetRepoPathIfAvailable(file);
					if (repoPath == null) {
						// source not available
						continue;
					}
					string url = Configuration.SOURCE_REPO_URL + repoPath;

					// GDB requires paths beginning with /src. Therefore, all source files must be merged into a common /src directory.
					string shortPath = file.Substring(1);
					if (shortPath.Contains("/src/")) {
						shortPath = shortPath.Substring(shortPath.IndexOf("/src/") + 1);
					}
					IFileInfo targetFile = filesystem.GetFile(Path.Combine(directory, shortPath));
					if (files.Contains(targetFile.FullName)) {
						continue;
					}
					files.Add(targetFile.FullName);
					tasks.Add(requestHandler.DownloadFromUrlAsync(url, targetFile.FullName, Configuration.SOURCE_REPO_AUTHENTICATION));
				}
			}
			foreach (Task t in tasks) {
				t.Wait();
			}
		}
	}
}
