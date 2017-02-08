using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.TagHelpers {
	public class PossibleLinkTagHelper : TagHelper {
		enum eTarget { _blank, _parent, _self, _top };
		public string Href { get; set; }
		public bool IsExternal { get; set; }
		public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output) {
			if (Uri.IsWellFormedUriString(Href, UriKind.Absolute)) {
				output.TagName = "a";
				output.Attributes.SetAttribute("href", Href);
				if (IsExternal) {
					output.Attributes.SetAttribute("target", "_blank");
				}
			} else {
				output.TagName = string.Empty;
				output.Attributes.Clear();
			}
			return base.ProcessAsync(context, output);
		}
	}
}
