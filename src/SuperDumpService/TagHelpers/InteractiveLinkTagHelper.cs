using Microsoft.AspNetCore.Razor.TagHelpers;
using SuperDump.Models;
using SuperDumpService.Models;
using SuperDumpService.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.TagHelpers {
	public class InteractiveLinkTagHelper : TagHelper {
		public ReportViewModel Model { get; set; }
		public string InteractiveGdbHost { get; set; }
		public DumpType Type { get; set; }
		public string DumpId { get; set; }
		public string BundleId { get; set; }
		public string Executable { get; set; }
		public string Command { get; set; }

		public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output) {
			InteractiveGdbHost = Model.InteractiveGdbHost;
			Type = Model.DumpType;
			DumpId = Model.DumpId;
			BundleId = Model.BundleId;
			Executable = (Model.Result?.SystemContext as SDCDSystemContext)?.FileName;

			if (string.IsNullOrEmpty(InteractiveGdbHost)) {
				output.TagName = null;
				output.Attributes.Clear();
			} else {
				string url;
				output.TagName = "a";
				if (Type == DumpType.LinuxCoreDump) {
					url = $"{InteractiveGdbHost}/?arg={BundleId}&arg={DumpId}&arg={Executable}";
					if (!string.IsNullOrEmpty(Command)) {
						url += $"&arg=\"{Command}\"";
					}
				} else {
					url = $"Interactive?bundleId={BundleId}&dumpId={DumpId}";
					if(!string.IsNullOrEmpty(Command)) {
						url += $"&cmd={Command}";
					}
				}
				output.Attributes.Add("href", url);
				output.Attributes.Add("target", "_blank");
			}
			return base.ProcessAsync(context, output);
		}
	}
}
