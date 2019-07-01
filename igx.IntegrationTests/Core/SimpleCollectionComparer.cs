using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.IntegrationTests.Core
{
    public class SimpleJsonComparer
    {
        public static bool IsEqual(JToken x, JToken y)
        {
            return IsEqual(x as JArray, y as JArray);
        }

        public static bool IsEqual(JArray x, JArray y)
        {
            if (x == y) return true;
            if (x.Count() != y.Count()) return false;

            for (int i = 0; i < x.Count(); i++)
            {
                if (!IsEqual(x.ElementAt(i) as JObject, y.ElementAt(i) as JObject)) return false;
            }

            return true;
        }

        public static bool IsEqual(JObject x, JObject y)
        {
            List<string> properties = new List<string>();
            foreach(var prop in x)
            {
                properties.Add(prop.Key);
            }

            foreach(var prop in properties)
            {
                object val1 = x[prop];
                object val2 = y[prop];

                if (val1 == null && val2 == null) continue;
                if ((val1 == null && val2 != null) || (val2 == null && val1 != null)) return false;

                if (val1.ToString() != val2.ToString()) return false;
            }

            return true;
        }
    }
}
