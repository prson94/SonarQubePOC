using System;
using System.Threading;

namespace d360.core.helpers
{
    public static class PluralCultureHelper
    {
        public static bool IsNeutralCultureEnglish()
        {
            var neutralCulture = Thread.CurrentThread.CurrentCulture.Parent.Name;
            var isNeutralCultureEnglish = neutralCulture.Equals("en", StringComparison.OrdinalIgnoreCase);
            return isNeutralCultureEnglish;
        }
    }
}
