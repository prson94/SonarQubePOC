using System;

using Newtonsoft.Json;

namespace d360.web.Models.Formatters
{
    public class GuidConverter : JsonConverter<Guid>
    {
        public override bool CanRead => false;

        public override void WriteJson(JsonWriter writer, Guid value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString().ToLower());
        }

        public override Guid ReadJson(JsonReader reader, Type objectType, Guid existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
