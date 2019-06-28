using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;
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
        public static StringContent AsStringContent(this JArray json)
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

        public static void UpdateValueOnProperty(this JToken @object, string property, string value)
        {
            @object[property] = value;
        }

        public static void UpdateValueOnProperty(this JToken @object, string property, bool value)
        {
            @object[property] = value;
        }

        public static void UpdateValueOnProperty(this JToken @object, string property, int value)
        {
            @object[property] = value;
        }

        public static void AppendValueOnProperty(this JToken @object, string property, string value)
        {
            @object[property] = @object[property] + value;
        }

        public static void AddNewToken(this JObject @object, string property, string value)
        {
            @object.Add(new JProperty(property, value));
        }

        public static bool AreEqualOnField(JObject o1, JObject o2, string prop, bool checkLowerCase = false)
        {
            if(checkLowerCase)
                return o1[prop].ToString().ToLower() == o2[prop].ToString().ToLower();

            return o1[prop].ToString() == o2[prop].ToString(); 
        }

        public static bool DoesContainFields(JToken obj, params string[] fields)
        {
            return DoesContainFields(obj as JObject, fields);
        }
        public static bool DoesContainFields(JObject obj, params string[] fields)
        {
            foreach(var prop in fields)
            {
                if (obj[prop] == null)
                    return false;
            }

            return true;
        }

    }
}
