using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace d360.core
{
    [DebuggerStepThrough]
    public static class Preconditions
    {
        private static void ValidateParameterName(string parameterName)
        {
            if (string.IsNullOrEmpty(parameterName))
            {
                throw new ArgumentException("Value should be not empty", nameof(parameterName));
            }
        }

        public delegate bool EqualityCheckDelegate<in T, W>(T value, W allowed);

        public static T Exists<T>(string parameterName, T actual, IEnumerable<T> expected, EqualityCheckDelegate<T, T> equalityCheck)
        {
            return Exists<T, T>(parameterName, actual, expected, equalityCheck);
        }

        public static TExpected Exists<T, TExpected>(string parameterName, T actual, IEnumerable<TExpected> expected, EqualityCheckDelegate<T, TExpected> equalityCheck)
        {
            ValidateParameterName(parameterName);

            TExpected result = expected.FirstOrDefault(x => equalityCheck(actual, x));
            if (result == null)
            {
                throw new ArgumentException($"{nameof(actual)} should be in {nameof(expected)} collection", parameterName);
            }
            return result;
        }

        public static string Exists(string parameterName, string actual, IEnumerable<string> expected, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            return Exists<string>(parameterName, actual, expected, (_, allowed) => string.Equals(actual, allowed, comparisonType));
        }

        public static T NotNull<T>(T value, string parameterName)
            where T : class
        {
            ValidateParameterName(parameterName);

            if (value is null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return value;
        }

        public static T NotNull<T>(T? value, string parameterName)
            where T : struct
        {
            ValidateParameterName(parameterName);

            if (value is null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return value.Value;
        }

        public static string NotEmpty(string value, string parameterName)
        {
            ValidateParameterName(parameterName);

            if (value is null)
            {
                throw new ArgumentNullException(parameterName);
            }

            if (value.Length == 0)
            {
                throw new ArgumentException("String value cannot be empty.", parameterName);
            }

            return value;
        }

        public static ICollection<T> NotEmpty<T>(ICollection<T> value, string parameterName)
        {
            ValidateParameterName(parameterName);

            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            if (value.Count == 0)
            {
                throw new ArgumentException("Collection should not be empty.", parameterName);
            }

            return value;
        }

        public static IReadOnlyCollection<T> NotEmpty<T>(IReadOnlyCollection<T> value, string parameterName)
        {
            ValidateParameterName(parameterName);

            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            if (value.Count == 0)
            {
                throw new ArgumentException("Collection should not be empty.", parameterName);
            }

            return value;
        }

        public static TEnum IsDefined<TEnum>(TEnum value, string parameterName)
            where TEnum : Enum
        {
            ValidateParameterName(parameterName);

            if (!Enum.IsDefined(typeof(TEnum), value))
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }

            return value;
        }
    }
}
