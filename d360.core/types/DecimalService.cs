using System;
using System.Collections.Generic;
using System.Globalization;

namespace d360.core.types
{
    public sealed class DecimalService : IDecimalService
    {
        private static readonly IFormatProvider DefaultProvider = CultureInfo.InvariantCulture;
        private static readonly NumberStyles DefaultStyles = NumberStyles.Any;

        public bool TryParse(IEnumerable<string> input, out IList<decimal> result, IFormatProvider provider, NumberStyles styles)
        {
            result = new List<decimal>();

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

        public bool TryParse(IEnumerable<string> input, out IList<decimal> result)
        {
            return TryParse(input, out result, DefaultProvider, DefaultStyles);
        }

        public bool CanParse(string input)
        {
            return CanParse(input, DefaultProvider, DefaultStyles);
        }

        public bool CanParse(string input, IFormatProvider provider, NumberStyles styles)
        {
            return TryParse(input, out _, provider, styles);
        }

        public decimal Parse(string input, IFormatProvider provider, NumberStyles styles)
        {
            if (TryParse(input, out var result, CultureInfo.InvariantCulture, styles))
            {
                return result;
            }

            throw new ArgumentOutOfRangeException(nameof(input), @"Can not parse input string");
        }

        public decimal Parse(string input)
        {
            return Parse(input, DefaultProvider, DefaultStyles);
        }

        public IEnumerable<decimal> Parse(IEnumerable<string> input, IFormatProvider provider, NumberStyles styles)
        {
            foreach (var value in input.Safe())
            {
                yield return Parse(value, provider, styles);
            }
        }

        public IEnumerable<decimal> Parse(IEnumerable<string> input)
        {
            return Parse(input, DefaultProvider, DefaultStyles);
        }

        public bool TryParse(string input, out decimal result, IFormatProvider provider, NumberStyles styles)
        {
            return decimal.TryParse(input, styles, provider, out result);
        }

        public bool TryParse(string input, out decimal result)
        {
            return TryParse(input, out result, DefaultProvider, DefaultStyles);
        }
    }
}
