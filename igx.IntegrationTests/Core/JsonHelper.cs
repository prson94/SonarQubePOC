using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace igx.IntegrationTests.Core
{
    public static class JsonHelper
    {
        public static StringContent AsStringContent(this JObject json)
        {
            return new StringContent(JsonConvert.SerializeObject(json), Encoding.UTF8, "application/json");
        }
        public static JObject AsJobject(this string rawJson)
        {
            return JObject.Parse(rawJson);
        }
        public static bool HasSameFieldValue(this JObject json, JToken token, string field)
        {
            try
            {
                return json[field].ToString() == token[field].ToString();
            }
            catch
            {
                return false;
            }
        }

        public static bool DoesContainToken(this IEnumerable<JToken> jTokens, JObject token)
        {
            if (jTokens == null || jTokens.Count() == 0) return false;
            int sameFields = 0;
            foreach (var item in jTokens)
            {
                sameFields = 0;
                foreach (var subItem in item.ToArray())
                {
                    var propName = subItem.ToObject<JProperty>().Name;
                    if (item[propName].ToString() == token[propName].ToString())
                        sameFields++;
                }

                if (item.Count() == sameFields) return true;
            }
            return false;
        }

        public static void UpdateValueOnProperty(this JObject @object, string property, string value)
        {
            @object[property] = value;
        }

        public static void AppendValueOnProperty(this JObject @object, string property, string value)
        {
            @object[property] = @object[property] + value;
        }


    }
}
