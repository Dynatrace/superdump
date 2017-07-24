using Microsoft.AspNetCore.Razor.TagHelpers;
using SuperDumpModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperDumpService.TagHelpers {
	public class DynatraceSourceLinkTagHelper : TagHelper {
		public string SourceFile { get; set; }
		public string RepositoryUrl { get; set; }

		public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output) {
			Console.WriteLine($"{SourceFile}: {RepositoryUrl}");
			if(SourceFile == null) {
				output.TagName = string.Empty;
				output.Attributes.Clear();
				output.Content.Clear();
			} else {
				if (IsLinkAvailable()) {
					output.TagName = "a";
					output.Attributes.Add("href", RepositoryUrl + GetLink());
				} else {
					output.TagName = string.Empty;
					output.Attributes.Clear();
				}
			}
			return base.ProcessAsync(context, output);
		}

		private bool IsLinkAvailable() {
			return IsLinkAvailable('\\') || IsLinkAvailable('/');
		}

		private bool IsLinkAvailable(char separator) {
			return !string.IsNullOrEmpty(RepositoryUrl) &&
				SourceFile.Contains($"{separator}agent{separator}native") && SourceFile.Contains("sprint_");
		}

		private string GetLink() {
			if (SourceFile.Contains("sprint_")) {
				int sprintOffset = SourceFile.IndexOf("sprint_");
				return "branches/" + SourceFile.Substring(sprintOffset);
			} else if (SourceFile.Contains("trunk")) {
				int trunkOffset = SourceFile.IndexOf("trunk");
				return SourceFile.Substring(trunkOffset);
			} else {
				return null;
			}
		}
	}
}
