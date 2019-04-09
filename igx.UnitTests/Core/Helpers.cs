using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
    }
}
