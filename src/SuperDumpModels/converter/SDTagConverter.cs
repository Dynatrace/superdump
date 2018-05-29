using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuperDump.Models;
using System;
using System.Reflection;

namespace SuperDumpModels {
	public class SDTagConverter : JsonConverter {
		public override bool CanConvert(Type objectType) {
			if (objectType.GetTypeInfo().IsAssignableFrom(typeof(SDTag).GetTypeInfo())) {
				return true;
			}
			return false;
		}

		public override object ReadJson(JsonReader reader,
			Type objectType, object existingValue, JsonSerializer serializer) {
			JObject item = JObject.Load(reader);
			var tag = JsonConvert.DeserializeObject<SDTag>(item.ToString());
			return SDTag.FixUpTagType(tag);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			throw new NotImplementedException();
		}
	}
}
