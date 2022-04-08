using System.Collections.Generic;
using System.Linq;

namespace d360.core.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Safe<T>(this IEnumerable<T> input)
        {
            return input ?? Enumerable.Empty<T>();
        }
    }
}
