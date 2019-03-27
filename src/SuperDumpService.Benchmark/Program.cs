using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using SuperDumpService.Benchmark.Benchmarks;

namespace SuperDumpService.Benchmark {
	public class Program {
		public static void Main(string[] args) {

			//var b = new SimilarityServiceBenchmarks();
			//await b.GlobalSetup();
			//b.ManySimilar();
			//b.ManySimilar();
			//b.ManySimilar();
			//b.ManySimilar();

			//var x = new MiniInfoBenchmarks();
			//x.GlobalSetup();
			//x.CreateMiniInfo();

			//BenchmarkRunner.Run<SimilarityCalculationBenchmarks>();
			//BenchmarkRunner.Run<SimilarityServiceBenchmarks>();

			BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
		}
	}
}
