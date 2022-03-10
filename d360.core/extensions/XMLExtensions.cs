using System.Collections.Generic;
using System.IO;

namespace d360.core
{
    public static class XMLExtensions
    {
        /// <summary>
        /// Replaces field token with their actual values from system lookups.
        /// </summary>
        /// <param name="xml">The XML that contains all the field names and values.</param>
        public static string ReplaceTokenWithValues(this string tokenFormatString, Dictionary<string, string> fields)
        {
            List<string> tokens = tokenFormatString.ParseTokens();
            // Format the text based on the tokens.
            foreach (string token in tokens)
            {
                var n = token.Substring(1, token.Length - 2);   // Name of the element to find in the XML.
                if (fields.ContainsKey(n))
                {
                    tokenFormatString = tokenFormatString.Replace(token, fields[n]);
                }
            }

            return tokenFormatString;
        }

        public static string GetSafeFilename(this string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return string.Empty;
            }

            //restricted characters check
            var fn = string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));

            // max filename check
            if (fn.Length > 250)
            {
                fn = fn.Substring(0, 250);
            }

            return fn;
        }
    }
}
