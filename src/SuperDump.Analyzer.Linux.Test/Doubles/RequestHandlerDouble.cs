using System;
using System.Threading.Tasks;
using SuperDump.Analyzer.Linux.Boundary;

namespace SuperDump.Doubles {
	class RequestHandlerDouble : IHttpRequestHandler {

		public string FromUrl { get; private set; }
		public string ToFile { get; private set; }
		public bool Return { get; set; } = false;

		public Task<bool> DownloadFromUrlAsync(string url, string targetFile) {
			this.FromUrl = url;
			this.ToFile = targetFile;
			return Task.FromResult<bool>(Return);
		}
	}
}
