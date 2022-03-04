using System;
using System.Collections.Generic;

using d360.core.entities;

using Newtonsoft.Json.Linq;

namespace d360.core
{
    public static class JsonExtensions
    {
        public static int GetNumberOfDecimalPlaces(this decimal d)
        {
            int count = BitConverter.GetBytes(decimal.GetBits(d)[3])[2];
            return count;
        }
        public static List<FieldJsonProperty> ParseJsonIntoJsonPropertiesCollection(this string o)
        {
            var token = JToken.Parse(o);
            return token.ParseJsonIntoJsonPropertiesCollection();
        }

        public static List<FieldJsonProperty> ParseJsonIntoJsonPropertiesCollection(this JToken o)
        {
            List<FieldJsonProperty> properties = new List<FieldJsonProperty>();

            if (o is JArray)
            {
                properties = (o as JArray).ParseJsonIntoJsonPropertiesCollection();
            }
            else if (o is JObject)
            {
                properties = (o as JObject).ParseJsonIntoJsonPropertiesCollection(0);
            }

            return properties;
        }

        private static List<FieldJsonProperty> ParseJsonIntoJsonPropertiesCollection(this JArray o)
        {
            List<FieldJsonProperty> properties = new List<FieldJsonProperty>();

            int pos = 0;
            foreach (JToken c in o)
            {
                properties.AddRange(
                    (c as JObject).ParseJsonIntoJsonPropertiesCollection(pos)
                    );
                pos++;
            }

            return properties;
        }

        private static List<FieldJsonProperty> ParseJsonIntoJsonPropertiesCollection(this JObject o, int position = 0)
        {
            List<FieldJsonProperty> properties = new List<FieldJsonProperty>();

            foreach (JProperty p in o.Properties())
            {
                if (p.Value is JArray)
                {
                    properties.Add(new FieldJsonProperty { IsArray = true, Name = p.Name, Path = p.Path, Position = position });

                    int pos = 0;
                    foreach (JToken c in p.Value)
                    {
                        if (c is JObject)
                        {
                            properties.AddRange(
                                (c as JObject).ParseJsonIntoJsonPropertiesCollection(pos)
                                );
                        }
                        pos++;
                    }
                }
                else if (p.Value is JObject)
                {
                    properties.Add(new FieldJsonProperty { IsArray = false, Name = p.Name, Path = p.Path, Position = position });
                    properties.AddRange(
                        (p.Value as JObject).ParseJsonIntoJsonPropertiesCollection(position)
                        );
                }
                else
                {
                    properties.Add(new FieldJsonProperty
                    {
                        IsArray = false,
                        Name = p.Name,
                        Path = p.Path,
                        Position = position,
                        Value = p.Value.ToString()
                    });
                }
            }

            return properties;
        }
    }
}
