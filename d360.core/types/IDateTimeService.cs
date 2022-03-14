using System;
using System.Collections.Generic;
using System.Globalization;

namespace d360.core.types
{
    /// <summary>
    /// Use | extend this class when you work with <see cref="DateTime"/> type.
    /// This will decrease about of code duplicates, make your code more stable and easy to change.
    /// </summary>
    public interface IDateTimeService
    {
        bool CanParse(string input);

        bool CanParse(string input, IFormatProvider formatProvider, DateTimeStyles dateTimeStyles);

        DateTime Parse(string input, IFormatProvider formatProvider, DateTimeStyles dateTimeStyles);

        DateTime Parse(string input);

        IEnumerable<DateTime> Parse(IEnumerable<string> input, IFormatProvider provider, DateTimeStyles styles);

        IEnumerable<DateTime> Parse(IEnumerable<string> input);

        bool TryParse(string input, out DateTime result, IFormatProvider formatProvider, DateTimeStyles dateTimeStyles);

        bool TryParse(string input, out DateTime result);

        bool TryParse(IEnumerable<string> input, out IList<DateTime> result, IFormatProvider provider, DateTimeStyles styles);

        bool TryParse(IEnumerable<string> input, out IList<DateTime> result);

        /// <summary>
        /// Provides current date time value including timezone information.
        /// This is exactly the type we should to use in the app instead of <see cref="DateTime"/>...
        /// </summary>
        /// <returns></returns>
        DateTimeOffset Now();
    }
}
