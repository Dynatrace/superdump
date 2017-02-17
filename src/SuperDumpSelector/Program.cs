using Microsoft.Diagnostics.Runtime;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Configuration;
using System.Reflection;

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
				var p = new Process();
				using (DataTarget target = DataTarget.LoadCrashDump(dumpfile)) {
					if (target.PointerSize == 8) {
						p.StartInfo.FileName = ResolvePath(ConfigurationManager.AppSettings["superdumpx64"]);
						if (!File.Exists(p.StartInfo.FileName)) p.StartInfo.FileName = ResolvePath(ConfigurationManager.AppSettings["superdumpx64_deployment"]);
						Console.WriteLine("detected x64 dump, selecting 64-bit build of SuperDump ...");
					} else if (target.PointerSize == 4) {
						p.StartInfo.FileName = ResolvePath(ConfigurationManager.AppSettings["superdumpx86"]);
						if (!File.Exists(p.StartInfo.FileName)) p.StartInfo.FileName = ResolvePath(ConfigurationManager.AppSettings["superdumpx86_deployment"]);
						Console.WriteLine("detected x86 dump, selecting 32-bit build of SuperDump ...");
					} else {
						Console.WriteLine("target dump architecture is different than x64 or x86, this is not yet supported!");
					}
				}
				p.StartInfo.Arguments = $"{dumpfile} {outputfile}";
				p.StartInfo.UseShellExecute = false;
				p.StartInfo.RedirectStandardOutput = true;
				p.StartInfo.WorkingDirectory = Path.GetDirectoryName(p.StartInfo.FileName);

				try {
					Console.WriteLine($"launching '{p.StartInfo.FileName}' '{p.StartInfo.Arguments}'");
					Console.WriteLine($"working dir: '{p.StartInfo.WorkingDirectory}'");
					p.Start();
					TrySetPriorityClass(p, ProcessPriorityClass.BelowNormal);
					Console.WriteLine("... analyzing");
					do {
						Console.WriteLine(p.StandardOutput.ReadLine());
						Thread.Sleep(15);
					} while (!p.HasExited);

					Console.Write(p.StandardOutput.ReadToEnd());
				} catch (Exception ex) {
					Console.WriteLine("Exception thrown, maybe you have not built SuperDump in right bitness yet? Build and try again");

					Console.WriteLine(ex.Message);
					throw;
				}
			} else {
				throw new FileNotFoundException($"Dump file was not found at {dumpfile}. Please try again");
			}
		}

		private static void TrySetPriorityClass(Process process, ProcessPriorityClass priority) {
			try {
				process.PriorityClass = priority;
			} catch (Exception) {
				// this might be disallowed, e.g. in Azure WebApps
			}
		}

		private static string ResolvePath(string relativePath) {
			string combinedPath = Path.Combine(Assembly.GetExecutingAssembly().CodeBase, relativePath);
			return Path.GetFullPath((new Uri(combinedPath)).LocalPath);
		}
	}
}
