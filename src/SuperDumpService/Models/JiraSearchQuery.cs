using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SuperDumpService.Models {
	public class JiraSearchQuery {
		public string Jql { get; set; }
		public string[] Fields { get; set; }
		public int StartAt { get; set; }
	}
}
