using igx.UnitTests;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace igx.UnitTests.Core
{

    class Helpers
    {
        public static bool NormalisedComparer(string s1, string s2)
        {
            var normalisedS1 = NormaliseString(s1);
            var normalisedS2 = NormaliseString(s2);
            return String.Equals(normalisedS1, normalisedS2, StringComparison.OrdinalIgnoreCase);
        }

        public static string NormaliseString(string s)
        {
            return Regex.Replace(s, @"\s+", " ");
        }

        public static bool IsTypeOf(Type type, JObject jObject)
        {
            var props = type.GetProperties();

            foreach (var item in jObject)
            {
                if (!props.Any(x => x.Name == item.Key)) return false;
            }

            return true;
        }

        public static bool IsTypeOf(Type type, JArray jArray)
        {
            var props = type.GetProperties();

            foreach (JObject jObject in jArray)
            {
                foreach (var item in jObject)
                {
                    if (!props.Any(x => x.Name == item.Key)) return false;
                }
            }

            return true;
        }


    }


}


namespace Xunit
{
    public class AssertJSON
    {
        public static void True<T>(string json)
        {
            string userMessage = XMsg.InvalidJSON;
            bool areEqual = false;
            try
            {
                var obj = JsonConvert.DeserializeObject<T>(json);
                var serialized = JsonConvert.SerializeObject(obj);
                areEqual = json == serialized;


            }
            catch (Exception ex)
            {
                userMessage = ex.Message;
            }


            Assert.True(areEqual, userMessage);
        }
    }
}

