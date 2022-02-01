using System;
using System.Collections.Generic;
using System.Globalization;

namespace d360.core.types
{
    public interface IInt32TypeService
    {
        bool CanParse(string input);

        bool CanParse(string input, IFormatProvider provider, NumberStyles styles);

        int Parse(string input, IFormatProvider provider, NumberStyles styles);

        int Parse(string input);

        IEnumerable<int> Parse(IEnumerable<string> input, IFormatProvider provider, NumberStyles styles);

        IEnumerable<int> Parse(IEnumerable<string> input);

        bool TryParse(string input, out int result, IFormatProvider provider, NumberStyles styles);

        bool TryParse(string input, out int result);

        bool TryParse(IEnumerable<string> input, out IList<int> result, IFormatProvider provider, NumberStyles styles);

        bool TryParse(IEnumerable<string> input, out IList<int> result);

        string ToString(int value);
        string ToString(int value, string format, IFormatProvider formatProvider);

        IEnumerable<string> ToString(IEnumerable<int> value);

        IEnumerable<string> ToString(IEnumerable<int> value, string format, IFormatProvider formatProvider);
    }
}