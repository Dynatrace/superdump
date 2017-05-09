using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuperDump.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;

namespace SuperDumpModels
{
    public class SDModuleConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            if (objectType.GetTypeInfo().IsAssignableFrom(typeof(SDModule).GetTypeInfo()))
            {
                return true;
            }
            return false;
        }

        public override object ReadJson(JsonReader reader,
            Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject item = JObject.Load(reader);
            if (item["StartAddress"] != null)
            {
                return JsonConvert.DeserializeObject<SDCDModule>(item.ToString());
            }
            else
            {
                return JsonConvert.DeserializeObject<SDModule>(item.ToString());
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
