using Microsoft.Diagnostics.Runtime;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Configuration;
using System.Reflection;
using System.Threading.Tasks;
using SuperDump.Common;
using CommandLine;
using Dynatrace.OneAgent.Sdk.Api;
using Dynatrace.OneAgent.Sdk.Api.Enums;

namespace SuperDumpSelector {
	public static class Program {
		private static IOneAgentSdk dynatraceSdk = OneAgentSdkFactory.CreateInstance();

		public static int Main(string[] args) {
			try {
				var result = Parser.Default.ParseArguments<Options>(args)
					.WithParsed(options => {
						var tracer = dynatraceSdk.TraceIncomingRemoteCall("Analyze", "SuperDumpSelector", "unknownserviceendpoint");
						tracer.SetDynatraceStringTag(options.TraceTag);
						tracer.Trace(() => RunAnalysis(options));
					});
			} catch (Exception e) {
				Console.Error.WriteLine($"SuperDumpSelector failed: {e}");
				return 1;
			}
			return 0;
		}

		private static void RunAnalysis(Options options) {
			string dumpfile = options.DumpFile;
			string outputfile = options.OutputFile;

			Console.WriteLine(Environment.CurrentDirectory);

			if (File.Exists(dumpfile)) {
				var superDumpPathInfo = FindSuperDumpPath(dumpfile);
				RunSuperDump(superDumpPathInfo, dumpfile, outputfile, options.TraceTag).Wait();
			} else {
				throw new FileNotFoundException($"Dump file was not found at {dumpfile}. Please try again");
			}
		}

		private static FileInfo FindSuperDumpPath(string dumpfile) {
			using (DataTarget target = DataTarget.LoadCrashDump(dumpfile)) {
				string superDumpPath;
				if (target.PointerSize == 8) {
					superDumpPath = ResolvePath(ConfigurationManager.AppSettings["superdumpx64"]);
					if (!File.Exists(superDumpPath)) superDumpPath = ResolvePath(ConfigurationManager.AppSettings["superdumpx64_deployment"]);
					Console.WriteLine("detected x64 dump, selecting 64-bit build of SuperDump ...");
				} else if (target.PointerSize == 4) {
					superDumpPath = ResolvePath(ConfigurationManager.AppSettings["superdumpx86"]);
					if (!File.Exists(superDumpPath)) superDumpPath = ResolvePath(ConfigurationManager.AppSettings["superdumpx86_deployment"]);
					Console.WriteLine("detected x86 dump, selecting 32-bit build of SuperDump ...");
				} else {
					throw new NotSupportedException("target dump architecture is different than x64 or x86, this is not yet supported!");
				}
				return new FileInfo(superDumpPath);
			}
		}

		private static async Task RunSuperDump(FileInfo superDumpPath, string dumpfile, string outputfile, string dtTraceTag) {
			var tracer = dynatraceSdk.TraceOutgoingRemoteCall("Analyze", "SuperDump", "unknownserviceendpoint", ChannelType.OTHER, superDumpPath.FullName);
			await tracer.TraceAsync(async () => {

				string[] arguments = {
					$"--dump \"{dumpfile}\"",
					$"--out \"{outputfile}\"",
					$"--tracetag \"{tracer.GetDynatraceStringTag()}\""
				};

				using (var process = await ProcessRunner.Run(superDumpPath.FullName, superDumpPath.Directory, arguments)) {
					//TrySetPriorityClass(process, ProcessPriorityClass.BelowNormal);
					Console.WriteLine($"stdout: {process.StdOut}");
					Console.WriteLine($"stderr: {process.StdErr}");
					Console.WriteLine($"exitcode: {process.ExitCode}");
					if (process.ExitCode != 0) {
						throw new SuperDumpFailedException(process.StdErr);
					}
				}
			});
		}

		private static string ResolvePath(string relativePath) {
			string combinedPath = Path.Combine(Assembly.GetExecutingAssembly().CodeBase, relativePath);
			return Path.GetFullPath((new Uri(combinedPath)).LocalPath);
		}
	}
}
