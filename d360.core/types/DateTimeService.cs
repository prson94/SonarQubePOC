using System;
using System.Collections.Generic;
using System.Globalization;

namespace d360.core.types
{
    public sealed class DateTimeService : IDateTimeService
    {
        private static readonly IFormatProvider DefaultProvider = CultureInfo.InvariantCulture;
        private static readonly DateTimeStyles DefaultStyles = DateTimeStyles.None;

        public bool CanParse(string input)
        {
            return CanParse(input, DefaultProvider, DefaultStyles);
        }

        public bool CanParse(string input, IFormatProvider formatProvider, DateTimeStyles dateTimeStyles)
        {
            return DateTime.TryParse(input, formatProvider, dateTimeStyles, out _);
        }

        public DateTime Parse(string input, IFormatProvider formatProvider, DateTimeStyles dateTimeStyles)
        {
            return DateTime.Parse(input, formatProvider, dateTimeStyles);
        }

        public DateTime Parse(string input)
        {
            return Parse(input, DefaultProvider, DefaultStyles);
        }

        public IEnumerable<DateTime> Parse(IEnumerable<string> input, IFormatProvider provider, DateTimeStyles styles)
        {
            foreach (var value in input.Safe())
            {
                yield return Parse(value, provider, styles);
            }
        }

        public IEnumerable<DateTime> Parse(IEnumerable<string> input)
        {
            return Parse(input, DefaultProvider, DefaultStyles);
        }

        public bool TryParse(string input, out DateTime result, IFormatProvider formatProvider, DateTimeStyles dateTimeStyles)
        {
            return DateTime.TryParse(input, formatProvider, dateTimeStyles, out result);
        }

        public bool TryParse(string input, out DateTime result)
        {
            return TryParse(input, out result, DefaultProvider, DefaultStyles);
        }

        public bool TryParse(IEnumerable<string> input, out IList<DateTime> result, IFormatProvider provider, DateTimeStyles styles)
        {
            result = new List<DateTime>();

            foreach (var value in input)
            {
                if (TryParse(value, out var valueResult, provider, styles))
                {
                    result.Add(valueResult);
                    continue;
                }

                return false;
            }

            return true;
        }

        public bool TryParse(IEnumerable<string> input, out IList<DateTime> result)
        {
            return TryParse(input, out result, DefaultProvider, DefaultStyles);
        }

        public DateTimeOffset Now()
        {
            return DateTimeOffset.Now;
        }
    }
}
