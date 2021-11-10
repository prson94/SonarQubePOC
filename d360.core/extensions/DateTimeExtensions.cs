using System;
using System.Globalization;

namespace d360.core
{
    public static class DateTimeExtensions
    {
        public static string ToShortDateString(this DateTime input, IFormatProvider formatProvider)
        {
            return input.ToString("d", formatProvider);
        }

        public static string ToShortDateStringInvariantCulture(this DateTime input)
        {
            return input.ToShortDateString(CultureInfo.InvariantCulture);
        }
    }
}