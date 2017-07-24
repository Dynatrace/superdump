using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.TagHelpers {
	public class GdbInteractiveLinkTagHelper : TagHelper {
		public string DumpId { get; set; }
		public string BundleId { get; set; }
		public string Executable { get; set; }
		public string Command { get; set; }

		public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output) {
			output.TagName = "a";
			// TODO replace 127.0.0.1 with settings.InteractiveGdbHost
			string url = $"http://127.0.0.1:3000/?arg={BundleId}&arg={DumpId}&arg={Executable}";
			if(!string.IsNullOrEmpty(Command)) {
				url += $"&arg={Command}";
			}
			output.Attributes.Add("href", url);
			return base.ProcessAsync(context, output);
		}
	}
}
