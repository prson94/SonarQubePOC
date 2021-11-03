using System;
using System.Collections.Generic;
using System.Globalization;

namespace d360.core.types
{
    /// <summary>
    /// Use | extend this class when you work with <see cref="long"/> type.
    /// This will decrease about of code duplicates, make your code more stable and easy to change.
    /// </summary>
    public interface IInt64Service
    {
        bool CanParse(string input);

        bool CanParse(string input, IFormatProvider provider, NumberStyles styles);

        long Parse(string input, IFormatProvider provider, NumberStyles styles);

        long Parse(string input);

        IEnumerable<long> Parse(IEnumerable<string> input, IFormatProvider provider, NumberStyles styles);

        IEnumerable<long> Parse(IEnumerable<string> input);

        bool TryParse(string input, out long result, IFormatProvider provider, NumberStyles styles);

        bool TryParse(string input, out long result);

        bool TryParse(IEnumerable<string> input, out IList<long> result, IFormatProvider provider, NumberStyles styles);

        bool TryParse(IEnumerable<string> input, out IList<long> result);
    }
}