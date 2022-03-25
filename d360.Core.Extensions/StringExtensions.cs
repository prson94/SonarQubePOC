using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using d360.core.resources;

namespace d360.core.Extensions
{
    public static class StringExtensions
    {
        public static bool In<T>(this T t, params T[] values)
        {
            return values.Contains(t);
        }

        public static string FormatBooleanReadOnlyValue(this bool b)
        {
            return b ? Values.BooleanTrue : Values.BooleanFalse;
        }

        /// <summary>
        /// Parses a string made up of one or more field tokens.
        /// </summary>
        /// <param name="tokenizedString">The string, for example: {FIELD_NAME}.{FIELD_NAME} - {FIELD_NAME}</param>
        /// <returns></returns>
        public static List<string> ParseTokens(this string tokenizedString)
        {
            var list = new List<string>();
            var r = new Regex(@"\{[\d\w]*\}", RegexOptions.Singleline);
            foreach (Match m in r.Matches(tokenizedString))
            {
                list.Add(m.Value);
            }

            return list;
        }

        public static string CleanForSql(this string text)
        {
            try
            {
                text = Regex.Replace(text, "'", "''");
            }
            catch
            { }

            return text;
        }

        public static string ReplaceLast(this string Source, string Find, string Replace)
        {
            int place = Source.LastIndexOf(Find);

            if (place == -1)
            {
                return Source;
            }

            return Source.Remove(place, Find.Length).Insert(place, Replace);
        }

        public static byte[] GetSha1Hash(this string inputString)
        {
            HashAlgorithm algorithm = SHA1.Create();  //or use SHA256.Create();
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        public static byte[] GetD3sHash(this string inputString)
        {
            HashAlgorithm algorithm = SHA256.Create();  //or use SHA256.Create();
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        public static string GetD3sHashString(this string inputString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetD3sHash(inputString))
            {
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Returns truncated string to 31 characters to accomodate xlsx sheet name limit
        /// </summary>
        public static string GetSafeSheetName(this string inputString)
        {
            if (inputString == null)
            {
                return "";
            }

            if (inputString.Length < 31)
            {
                return inputString;
            }

            return inputString.Substring(0, 28) + "...";
        }

        /// <summary>
        /// Checks if a string is a valid RGB color value.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <returns>Returns true if valid RGB; otherwise false.</returns>
        public static bool IsValidRgb(this string value)
        {
            if (value == null)
            {
                return true;
            }
            var colorRegex = new Regex("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");
            return colorRegex.IsMatch(value);
        }

        /// <summary>
        /// Used by Themeing endpoints. Add a variable to generated CSS output.
        /// </summary>
        /// <param name="builder">The StringBuilder being used to generate the CSS text</param>
        /// <param name="name">The property name, typially the JSON property name from the Theme.cs file.</param>
        /// <param name="value">The property value stored in the Theme record.</param>
        public static void AppendCssVariable(this StringBuilder builder, string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                builder.AppendLine($" --{name}: {value};");
            }
        }
    }
}
