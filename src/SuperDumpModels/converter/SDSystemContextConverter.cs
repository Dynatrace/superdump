using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuperDump.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;

namespace SuperDumpModels {
	public class SDSystemContextConverter : JsonConverter {
		public override bool CanConvert(Type objectType) {
			return objectType.GetTypeInfo().IsAssignableFrom(typeof(SDSystemContext).GetTypeInfo());
		}

		public override object ReadJson(JsonReader reader,
			Type objectType, object existingValue, JsonSerializer serializer) {
			JObject item = JObject.Load(reader);
			if (item["Uid"] != null) {
				return JsonConvert.DeserializeObject<SDCDSystemContext>(item.ToString(), new SDModuleConverter());
			} else {
				return JsonConvert.DeserializeObject<SDSystemContext>(item.ToString());
			}
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			throw new NotImplementedException();
		}
	}
}
