using Microsoft.Diagnostics.Runtime;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Configuration;
using System.Reflection;
using System.Threading.Tasks;
using SuperDump.Common;

namespace SuperDumpSelector {
	public static class Program {
		public static void Main(string[] args) {
			string dumpfile;
			if (args.Length > 0) {
				dumpfile = args[0];
			} else {
				Console.Write("Enter dump file path: ");
				dumpfile = Console.ReadLine();
			}
			string outputfile;
			if (args.Length > 1) {
				outputfile = args[1];
			} else {
				Console.Write("Enter output file path: ");
				outputfile = Console.ReadLine();
			}

			Console.WriteLine(Environment.CurrentDirectory);
			if (File.Exists(dumpfile)) {
				var superDumpPathInfo = FindSuperDumpPath(dumpfile);
				RunSuperDump(superDumpPathInfo, dumpfile, outputfile).Wait();
			} else {
				throw new FileNotFoundException($"Dump file was not found at {dumpfile}. Please try again");
			}
		}

		private static FileInfo FindSuperDumpPath(string dumpfile) {
			using (DataTarget target = DataTarget.LoadCrashDump(dumpfile)) {
				string superDumpPath; ;
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

		private static async Task RunSuperDump(FileInfo superDumpPath, string dumpfile, string outputfile) {
			using (var process = await ProcessRunner.Run(superDumpPath.FullName, superDumpPath.Directory, $"{dumpfile} {outputfile}")) {
				//TrySetPriorityClass(process, ProcessPriorityClass.BelowNormal);
				Console.WriteLine($"stdout: {process.StdOut}");
				Console.WriteLine($"stderr: {process.StdErr}");
				Console.WriteLine($"exitcode: {process.ExitCode}");
				if (process.ExitCode != 0) {
					throw new SuperDumpFailedException(process.StdErr);
				}
			}
		}

		private static string ResolvePath(string relativePath) {
			string combinedPath = Path.Combine(Assembly.GetExecutingAssembly().CodeBase, relativePath);
			return Path.GetFullPath((new Uri(combinedPath)).LocalPath);
		}
	}
}
