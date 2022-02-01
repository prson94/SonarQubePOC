using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace d360.core.types
{
    public sealed class Int32TypeService : IInt32TypeService
    {
        private static readonly IFormatProvider DefaultProvider = CultureInfo.InvariantCulture;
        private static readonly NumberStyles DefaultStyles = NumberStyles.Any;

        public bool TryParse(IEnumerable<string> input, out IList<int> result, IFormatProvider provider, NumberStyles styles)
        {
            result = new List<int>();

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

        public bool TryParse(IEnumerable<string> input, out IList<int> result)
        {
            return TryParse(input, out result, DefaultProvider, DefaultStyles);
        }

        public string ToString(int value)
        {
            return value.ToString(DefaultProvider);
        }

        public string ToString(int value, string format, IFormatProvider formatProvider)
        {
            return value.ToString(format, formatProvider);
        }

        public IEnumerable<string> ToString(IEnumerable<int> value)
        {
            return value.Safe().Select(ToString);
        }

        public IEnumerable<string> ToString(IEnumerable<int> value, string format, IFormatProvider formatProvider)
        {
            return value.Safe().Select(x => ToString(x, format, formatProvider));
        }

        public bool CanParse(string input)
        {
            return CanParse(input, DefaultProvider, DefaultStyles);
        }

        public bool CanParse(string input, IFormatProvider provider, NumberStyles styles)
        {
            return TryParse(input, out _, provider, styles);
        }

        public int Parse(string input, IFormatProvider provider, NumberStyles styles)
        {
            if (TryParse(input, out var result, CultureInfo.InvariantCulture, styles))
            {
                return result;
            }

            throw new ArgumentOutOfRangeException(nameof(input), @"Can not parse input string");
        }

        public int Parse(string input)
        {
            return Parse(input, DefaultProvider, DefaultStyles);
        }

        public IEnumerable<int> Parse(IEnumerable<string> input, IFormatProvider provider, NumberStyles styles)
        {
            foreach (var value in input.Safe())
            {
                yield return Parse(value, provider, styles);
            }
        }

        public IEnumerable<int> Parse(IEnumerable<string> input)
        {
            return Parse(input, DefaultProvider, DefaultStyles);
        }

        public bool TryParse(string input, out int result, IFormatProvider provider, NumberStyles styles)
        {
            return int.TryParse(input, styles, provider, out result);
        }

        public bool TryParse(string input, out int result)
        {
            return TryParse(input, out result, DefaultProvider, DefaultStyles);
        }
    }
}