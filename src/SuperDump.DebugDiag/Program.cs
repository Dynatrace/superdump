using CommandLine;
using DebugDiag.DotNet;
using Dynatrace.OneAgent.Sdk.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SuperDump.DebugDiag {
	class Program {
		private static IOneAgentSdk dynatraceSdk = OneAgentSdkFactory.CreateInstance();
		private static Stopwatch stopwatch = new Stopwatch();

		[STAThread]
		static void Main(string[] args) {
			var result = Parser.Default.ParseArguments<Options>(args)
				.WithParsed(options => {
					var tracer = dynatraceSdk.TraceIncomingRemoteCall("DebugDiagAnalysis", "SuperDump.DebugDiag.exe", "SuperDump.DebugDiag.exe");
					tracer.SetDynatraceStringTag(options.TraceTag);
					tracer.Trace(() => RunAnalysis(options));
				});
		}

		private static void PrintError(string msg, params object []args) {
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("ERROR: {0}", string.Format(msg, args));
			Console.ResetColor();
		}

		private static void RunAnalysis(Options options) {
			var dumpFile = new FileInfo(options.DumpFile);
			if (!dumpFile.Exists) {
				PrintError("Dump file does not exist: {0}", dumpFile);
				return;
			}

			var reportPath = new FileInfo(options.ReportFile);
			if (reportPath.Exists && options.Overwrite == false) {
				PrintError("Report file does already exist");
				return;
			}

			if (string.IsNullOrEmpty(options.SymbolPath)) {
				options.SymbolPath = Environment.GetEnvironmentVariable("_NT_SYMBOL_PATH");
			}

			using (var analyzer = new NetAnalyzer()) {
				//analyzer.Initialize(true, true, true, true);
				analyzer.AddDumpFile(dumpFile.FullName, options.SymbolPath);

				if (options.AnalysisRules == null ||options.AnalysisRules.Count() == 0) {
					options.AnalysisRules = new List<string>() {
						"CrashHangAnalysis",
						"MemoryAnalysis",
						"DotNetMemoryAnalysis"
					};
				}

				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.WriteLine("DebugDiag analysis");
				Console.ResetColor();

				Console.WriteLine("Dump: {0}", dumpFile);
				Console.WriteLine("Report file: {0}", options.ReportFile);
				Console.WriteLine("Analysis rules: {0}", string.Join(",", options.AnalysisRules.ToArray()));
				Console.WriteLine("Symbol path: {0}", options.SymbolPath);

				foreach (Type analysisRule in DebugDiagHelper.GetAnalysisRules(options.AnalysisRules))
					analyzer.AddAnalysisRuleToRunList(analysisRule);

				NetProgress progress = new NetProgress();
				try {
					stopwatch.Start();
					progress.OnSetCurrentStatusChanged += Progress_OnSetCurrentStatusChanged;
					progress.OnSetOverallStatusChanged += Progress_OnSetOverallStatusChanged;
					progress.OnEnd += Progress_OnEnd;

					Console.ForegroundColor = ConsoleColor.Cyan;
					Console.WriteLine("Start DebugDiag analysis");
					Console.ResetColor();

					analyzer.RunAnalysisRules(progress, options.SymbolPath, options.ImagePath, options.ReportFile);
				} finally {
					stopwatch.Stop();
				}
				progress.End();
			}
		}

		private static void Progress_OnEnd(object sender, EventArgs e) {
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(string.Format("\n\rTotal time of analysis: {0}", stopwatch.Elapsed.ToString()));
			Console.ResetColor();
		}

		private static void Progress_OnSetOverallStatusChanged(object sender, SetOverallStatusEventArgs e) {
			if (!string.IsNullOrEmpty(e.NewStatus)) {
				Console.WriteLine(string.Format("{0} - {1}", DateTime.Now.ToLongTimeString(), e.NewStatus));
			}
		}

		private static void Progress_OnSetCurrentStatusChanged(object sender, SetCurrentStatusEventArgs e) {
			if (!string.IsNullOrEmpty(e.NewStatus)) {
				Console.WriteLine(string.Format("{0} - {1}", DateTime.Now.ToLongTimeString(), e.NewStatus));
			}
		}
	}
}
