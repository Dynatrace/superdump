using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuperDump.Models;
using System;
using System.Reflection;

namespace SuperDumpModels {
	public class SDCombinedStackFrameConverter : JsonConverter {
		public override bool CanConvert(Type objectType) {
			if (objectType.GetTypeInfo().IsAssignableFrom(typeof(SDCombinedStackFrame).GetTypeInfo())) {
				return true;
			}
			return false;
		}

		public override object ReadJson(JsonReader reader,
			Type objectType, object existingValue, JsonSerializer serializer) {
			JObject item = JObject.Load(reader);
			if (item["Args"] != null) {
				return JsonConvert.DeserializeObject<SDCDCombinedStackFrame>(item.ToString());
			} else {
				return JsonConvert.DeserializeObject<SDCombinedStackFrame>(item.ToString());
			}
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			throw new NotImplementedException();
		}
	}
}
