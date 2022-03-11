using System;
using System.Collections.Generic;

namespace d360.core.helpers
{
    public class LowercaseStringEqualityComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            if (string.Equals(x, y, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        public int GetHashCode(string obj)
        {
            return obj.ToLower().GetHashCode();
        }
    }
}
