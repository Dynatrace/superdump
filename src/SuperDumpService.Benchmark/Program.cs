using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using SuperDumpService.Benchmark.Benchmarks;

namespace SuperDumpService.Benchmark {
	public class Program {
		public static void Main(string[] args) {

			//var b = new SimilarityServiceBenchmarks();
			//b.ManySimilar();

			//BenchmarkRunner.Run<SimilarityCalculationBenchmarks>();
			BenchmarkRunner.Run<SimilarityServiceBenchmarks>();
			//BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
		}
	}
}
