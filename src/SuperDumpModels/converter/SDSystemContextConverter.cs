using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuperDump.Models;
using System;
using System.Reflection;

namespace SuperDumpModels {
	public class SDSystemContextConverter : JsonConverter {
		public override bool CanConvert(Type objectType) {
			return objectType.GetTypeInfo().IsAssignableFrom(typeof(SDSystemContext).GetTypeInfo());
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			try {
				JObject item = JObject.Load(reader);
				if (item["Uid"] != null) {
					return JsonConvert.DeserializeObject<SDCDSystemContext>(item.ToString(), new SDModuleConverter());
				} else {
					return JsonConvert.DeserializeObject<SDSystemContext>(item.ToString());
				}
			} catch (Exception e) {
				return null;
			}
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			throw new NotImplementedException();
		}
	}
}
