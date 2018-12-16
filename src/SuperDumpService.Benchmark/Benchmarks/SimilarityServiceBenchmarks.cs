using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Options;
using SuperDump.Models;
using SuperDumpService.Benchmarks.Fakes;
using SuperDumpService.Helpers;
using SuperDumpService.Models;
using SuperDumpService.Services;

namespace SuperDumpService.Benchmark.Benchmarks {
	[ShortRunJob]
	public class SimilarityServiceBenchmarks {

		private readonly int N;
		private readonly SimilarityService similarityService;
		private readonly PathHelper pathHelper;
		private readonly FakeDumpStorage dumpStorage;
		private readonly DumpRepository dumpRepo;
		private readonly FakeRelationshipStorage relationshipStorage;
		private readonly RelationshipRepository relationshipRepo;

		public SimilarityServiceBenchmarks() {
			/// fake a repository of N very similar dumps. Then let similarity calculation run
			/// simulate filesystem access with Thread.Sleep in FakeDumpStorage
			N = 2000;

			var settings = Options.Create(new SuperDumpSettings {
				SimilarityDetectionEnabled = true,
				SimilarityDetectionMaxDays = 30
			});

			this.pathHelper = new PathHelper("", "", "");
			this.dumpStorage = new FakeDumpStorage(CreateFakeDumps(N));
			this.dumpRepo = new DumpRepository(dumpStorage, pathHelper);
			this.relationshipStorage = new FakeRelationshipStorage();
			this.relationshipRepo = new RelationshipRepository(relationshipStorage, dumpRepo, settings);
			this.similarityService = new SimilarityService(dumpRepo, relationshipRepo, settings);
		}

		[GlobalSetup]
		public async Task GlobalSetup() {
			// populate in-memory repository from fake storage
			for (int i = 0; i < N; i++) {
				await this.dumpRepo.PopulateForBundle($"bundle{i}");
			}

			// populate miniinfo cache
			for (int i = 0; i < N; i++) {
				await this.dumpRepo.GetMiniInfo(new DumpIdentifier($"bundle{i}", $"dump{i}"));
			}
			this.dumpRepo.SetIsPopulated();

			// enable storage delay simulation
			this.dumpStorage.DelaysEnabled = true;
			this.relationshipStorage.DelaysEnabled = true;
		}

		private IEnumerable<FakeDump> CreateFakeDumps(int n) {
			var rand = new Random();
			for (int i = 0; i < n; i++) {
				var res = new SDResult { ThreadInformation = new Dictionary<uint, SDThread>() };
				res.ThreadInformation[1] = new SDThread(1) {
					StackTrace = new SDCombinedStackTrace(new List<SDCombinedStackFrame>() {
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyErrorFrameA", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "app.dll", "MyAppFrameA", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "app.dll", "MyAppFrameB", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrameA", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrameB", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MyFrameworkFrameC", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MySystemFrameA", 123, 456, 789, 1011, 1213, null),
						new SDCombinedStackFrame(StackFrameType.Native, "ntdll.dll", "MySystemFrameA_" + rand.NextDouble(), 123, 456, 789, 1011, 1213, null), // add some slight difference
					})
				};
				AddTagToFrameAndThread(res, SDTag.NativeExceptionTag);

				yield return new FakeDump {
					MetaInfo = new DumpMetainfo { BundleId = $"bundle{i}", DumpId = $"dump{i}", Status = DumpStatus.Finished },
					FileInfo = null,
					Result = res,
					MiniInfo = CrashSimilarity.SDResultToMiniInfo(res)
				};
			}
		}

		private static void AddTagToFrameAndThread(SDResult result, SDTag tag) {
			result.ThreadInformation[1].Tags.Add(tag);
			result.ThreadInformation[1].StackTrace[0].Tags.Add(tag);
		}

		[Benchmark]
		public void ManySimilar() {
			similarityService.CalculateSimilarity(dumpRepo.Get(new DumpIdentifier("bundle1", "dump1")), true, DateTime.MinValue);
		}
	}
}
