using System;
using System.Collections.Generic;
using System.Globalization;

namespace d360.core.types
{
    /// <summary>
    /// Use | extend this class when you work with <see cref="Decimal"/> type.
    /// This will decrease about of code duplicates, make your code more stable and easy to change.
    /// </summary>
    public interface IDecimalService
    {
        bool CanParse(string input);

        bool CanParse(string input, IFormatProvider provider, NumberStyles styles);

        decimal Parse(string input, IFormatProvider provider, NumberStyles styles);

        decimal Parse(string input);

        IEnumerable<decimal> Parse(IEnumerable<string> input, IFormatProvider provider, NumberStyles styles);

        IEnumerable<decimal> Parse(IEnumerable<string> input);

        bool TryParse(string input, out decimal result, IFormatProvider provider, NumberStyles styles);

        bool TryParse(string input, out decimal result);

        bool TryParse(IEnumerable<string> input, out IList<decimal> result, IFormatProvider provider, NumberStyles styles);

        bool TryParse(IEnumerable<string> input, out IList<decimal> result);
    }
}