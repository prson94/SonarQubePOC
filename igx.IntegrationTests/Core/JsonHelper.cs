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
        public static StringContent AsStringContent(this string json)
        {
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        public static bool HasSameFieldValue(this string json, JToken token, string field)
        {
            try
            {
                return JsonConvert.DeserializeObject<JToken>(json)[field].ToString() == token[field].ToString();
            }
            catch
            {
                return false;
            }
        }

        public static string GetJTokenValue(this string json, string field)
        {
            return JsonConvert.DeserializeObject<JToken>(json)[field].ToString();
        }

        public static bool DoesContainToken(this IEnumerable<JToken> jTokens, string json)
        {
            if (jTokens == null || jTokens.Count() == 0) return false;
            var token = JsonConvert.DeserializeObject<JToken>(json);
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

        public static JToken GetBy(this IEnumerable<JToken> jTokens, string field, string value)
        {
            if (jTokens == null || jTokens.Count() == 0) return null;
            foreach (var item in jTokens)
            {
                if (item[field].ToString() == value)
                    return item;
            }
            return null;
        }

        public static string UpdateJsonOnField(string json, string field, string newValue)
        {
            var token = JsonConvert.DeserializeObject<JToken>(json);
            token[field] = newValue;
            return JsonConvert.SerializeObject(token);
        }

        public static string AppendJsonOnField(string json, string field, string newValue)
        {
            var token = JsonConvert.DeserializeObject<JToken>(json);
            token[field] = token[field].ToString() + newValue;
            return JsonConvert.SerializeObject(token);
        }

        public static string AddNewToken(string json, string prop, string value)
        {
            var token = JsonConvert.DeserializeObject<JToken>(json);
            var newProp = new JProperty(prop, value);
            token.First.AddAfterSelf(newProp);
            return JsonConvert.SerializeObject(token);
        }
    }
}
