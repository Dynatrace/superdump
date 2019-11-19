using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SuperDumpService.Models;

namespace SuperDumpService.Services.Analyzers {
	public class EmptyAnalyzerJob : InitalAnalyzerJob {
		private readonly DumpRepository dumpRepository;

		public EmptyAnalyzerJob(DumpRepository dumpRepository) {
			this.dumpRepository = dumpRepository;
		}

		public override Task<IEnumerable<DumpMetainfo>> CreateDumpInfos(string bundleId, DirectoryInfo directory) {
			//Should only create a dump if it is the lowest directory level and it is not empty.
			if (!directory.GetDirectories().Any() && directory.GetFiles().Any(file => !UnpackService.IsSupportedArchive(file.Name))) {
				return Task.FromResult(Enumerable.Repeat(dumpRepository.CreateEmptyDump(bundleId), 1));
			}
			return Task.FromResult(Enumerable.Empty<DumpMetainfo>());
		}

		public override Task<AnalyzerState> AnalyzeDump(DumpMetainfo dumpInfo, string analysisWorkingDir, AnalyzerState previousState) {
			if (dumpInfo.DumpType == DumpType.Empty) {
				return Task.FromResult(AnalyzerState.Cancel);
			} else {
				return Task.FromResult(previousState);
			}
		}
	}
}
