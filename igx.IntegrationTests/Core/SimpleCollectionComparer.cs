using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.IntegrationTests.Core
{
    public class SimpleCollectionComparere
    {
        public static bool IsEqual<T>(IEnumerable<T> x, IEnumerable<T> y)
        {
            if (x == y) return true;
            if (x.Count() != y.Count()) return false;

            for (int i = 0; i < x.Count(); i++)
            {
                if (!Compare(x.ElementAt(i), y.ElementAt(i))) return false;
            }

            return true;
        }

        private static bool Compare<T>(T x, T y)
        {
            foreach(var prop in x.GetType().GetProperties())
            {
                object val1 = prop.GetValue(x, null);
                object val2 = prop.GetValue(y, null);

                if (val1 == null && val2 == null) continue;
                if ((val1 == null && val2 != null) || (val2 == null && val1 != null)) return false;

                if (val1.ToString() != val2.ToString()) return false;
            }

            return true;
        }
    }
}
