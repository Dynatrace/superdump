using Nest;
using SuperDump.Models;
using SuperDumpService.Helpers;
using SuperDumpService.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace SuperDumpService.Services {
	public class ElasticSDResult {
		public static ElasticSDResult FromResultOrDefault(SDResult result, BundleMetainfo bundleInfo, DumpMetainfo dumpInfo, PathHelper pathHelper) {
			try {
				return FromResult(result, bundleInfo, dumpInfo, pathHelper);
			} catch (Exception e) {
				Console.Error.WriteLine($"ElasticSDResult.FromResult failed for {dumpInfo.Id}: {e.ToString()}");
			}
			return null;
		}

		public static ElasticSDResult FromResult(SDResult result, BundleMetainfo bundleInfo, DumpMetainfo dumpInfo, PathHelper pathHelper) {
			if (result == null) throw new ArgumentNullException("result");
			var eResult = new ElasticSDResult() {
				BundleId = dumpInfo.BundleId,
				DumpId = dumpInfo.DumpId,
				Timestamp = dumpInfo.Created,
				Type = dumpInfo.DumpType.ToString(),
				Executable = (result.SystemContext as SDCDSystemContext)?.FileName ?? "",
				IsManaged = result.IsManagedProcess,
				ProcessArchitecture = result.SystemContext?.ProcessArchitecture,
				SystemArchitecture = result.SystemContext?.SystemArchitecture,
				NrThreads = result.ThreadInformation != null ? result.ThreadInformation.Count : 0,
				LastEventDescription = result.LastEvent?.Description,
				LoadedModules = result.SystemContext?.DistinctModules().Select(m => m.FileName).Aggregate("", (m1, m2) => m1 + " " + m2),
				LoadedModulesVersioned = result.SystemContext?.DistinctModules().Select(m => $"{m.FileName}:{m.Version ?? "-"}").Aggregate("", (m1, m2) => m1 + " " + m2),
				DynatraceLoadedModulesVersioned = result.SystemContext?.DistinctModules().Where(m => m.Tags.Contains(SDTag.DynatraceAgentTag)).Select(m => $"{m.FileName}:{m.Version ?? "-"}").Aggregate("", (m1, m2) => m1 + " " + m2),
				ExitType = result.LastEvent?.Type ?? ""
			};

			bundleInfo.CustomProperties.TryGetValue("ref", out string reference);
			eResult.Reference = reference;
			
			bundleInfo.CustomProperties.TryGetValue("tenantId", out string tenantId);
			eResult.TenantId = tenantId;

			eResult.FaultingStacktrace = result.GetErrorOrLastExecutingThread()?.ToText();
			eResult.Stacktraces = ""; // TODO

			if (dumpInfo.Finished != null && dumpInfo.Started != null) {
				int durationSecs = (int)dumpInfo.Finished.Subtract(dumpInfo.Started).TotalSeconds;
				if (durationSecs > 0) {
					// Only if a valid duration is present
					eResult.AnalyzationDuration = durationSecs;
				}
			}

			long dumpSize = ComputeDumpFileSizeKb(bundleInfo, dumpInfo, pathHelper);
			if (dumpSize >= 0) {
				eResult.DumpSizeKb = dumpSize;
			}
			return eResult;
		}

		[Keyword(Name = "Id")]
		public string Id {
			get {
				return this.BundleId + "/" + this.DumpId;
			}
		}

		// Meta Information
		[Keyword(Name = "bundleId")]
		public string BundleId { get; set; }
		[Keyword(Name = "dumpId")]
		public string DumpId { get; set; }
		[Date(Name = "timestamp")]
		public DateTime Timestamp { get; set; }
		[Keyword(Name = "type")]
		public string Type { get; set; }
		[Keyword(Name = "ref")]
		public string Reference { get; set; }
		[Number(Name = "analyzationDurationSecs")]
		public int? AnalyzationDuration { get; set; }

		// Dump Information
		[Keyword(Name = "executable")]
		public string Executable { get; set; }
		[Boolean(Name = "isManaged")]
		public bool IsManaged { get; set; }
		[Keyword(Name = "processArch")]
		public string ProcessArchitecture { get; set; }
		[Keyword(Name = "systemArch")]
		public string SystemArchitecture { get; set; }
		[Number(Name = "nrThreads")]
		public int NrThreads { get; set; }
		[Keyword(Name = "exitType")]
		public string ExitType { get; set; }

		// Module Information
		[Keyword(Name = "lastEventDescription")]
		public string LastEventDescription { get; set; }
		[Text(Name = "loadedModules", Fielddata = true, Analyzer = "whitespace")]
		public string LoadedModules { get; set; }
		[Text(Name = "loadedModulesVersioned", Fielddata = true, Analyzer = "whitespace")]
		public string LoadedModulesVersioned { get; set; }

		/** dynatrace specific **/
		[Text(Name = "dynatraceLoadedModulesVersioned", Fielddata = true, Analyzer = "whitespace")]
		public string DynatraceLoadedModulesVersioned { get; set; }
		[Keyword(Name = "tenantId")]
		public string TenantId { get; set; }
		/** dynatrace specific **/

		[Number(Name = "dumpSize")]
		public long DumpSizeKb { get; set; }

		[Text(Name = "faultingStacktrace")]
		public string FaultingStacktrace { get; set; }

		[Text(Name = "stacktraces")]
		public string Stacktraces { get; set; }

		private static long ComputeDumpFileSizeKb(BundleMetainfo bundleInfo, DumpMetainfo dumpInfo, PathHelper pathHelper) {
			long dumpSize = -1;
			string dumpDirectory = pathHelper.GetDumpDirectory(bundleInfo.BundleId, dumpInfo.DumpId);
			if (!Directory.Exists(dumpDirectory)) {
				return -1;
			}
			foreach (var file in Directory.EnumerateFiles(dumpDirectory)) {
				if (file.EndsWith(".core.gz") || file.EndsWith(".dmp")) {
					FileInfo fi = new FileInfo(file);
					dumpSize = fi.Length;
					break;
				}
			}
			if(dumpSize == -1) {
				// No dump found
				return -1;
			}
			return dumpSize / 1024;
		}
	}

	public static class SDThreadExtensions {
		public static string ToText(this SDThread thread) {
			if (thread == null) return string.Empty;
			var sb = new StringBuilder();
			foreach(var frame in thread.StackTrace) {
				sb.AppendLine(frame.ToString());
			}
			return sb.ToString();
		}
	}
}
