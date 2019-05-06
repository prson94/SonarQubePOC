using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Data;
using d360.core.resources;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace d360.core
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

        public static string Decode(this string text)
        {
            text = text.Trim();
            return System.Web.HttpContext.Current.Server.HtmlDecode(text);
        }

        public static string Encode(this string text)
        {
            text = text.Trim();
            return System.Web.HttpContext.Current.Server.HtmlEncode(text);
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

        public static string StripFormatting(this string text, int? length)
        {
            try
            {
                text = Regex.Replace(text, "<[^>]*>", "");
                text = Regex.Replace(text, "&[^>]*;", "");
                text = text.Replace("\r", "");
                text = text.Replace("\n", "");

                if (length.HasValue)
                {
                    if (text.Length > length)
                    {
                        text = text.Substring(0, length.Value);
                        text += "...";
                    }
                }
            }
            catch
            { }

            return text;
        }

        public static string ReplaceFirst(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        public static string ReplaceLast(this string Source, string Find, string Replace)
        {
            int place = Source.LastIndexOf(Find);

            if (place == -1)
                return Source;

            return Source.Remove(place, Find.Length).Insert(place, Replace);
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
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }
    }

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
                    tokenFormatString = tokenFormatString.Replace(token, fields[n]);
            }

            return tokenFormatString;
        }

        public static XElement StripNamespaces(this XElement root)
        {
            var attributes = root.Attributes();
            attributes.Where(i => i.Name == "xmlns").Remove();
            return new XElement(
                root.Name.LocalName,
                attributes.Where(i => i.Name != "xmlns"),
                root.HasElements ?
                    root.Elements().Select(el => StripNamespaces(el)) :
                    (object)root.Value
            );
        }

        public static string GetSafeFilename(this string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return string.Empty;

            //restricted characters check
            var fn = string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));

            // max filename check
            if (fn.Length > 250)
                fn = fn.Substring(0, 250);

            return fn;
        }

        public static XElement ToXElement(this string xml)
        {
            return XElement.Parse(xml);
        }
    }

    public static class GeneralExtensions
    {
        public static string GetFullExceptionData(this Exception ex, bool includeStacktrace = true)
        {
            if (ex.InnerException != null && ex.InnerException.InnerException != null && ex.InnerException.InnerException.GetType() == typeof(SqlException))
            {
                SqlException sqlException = (SqlException)ex.InnerException.InnerException;

                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                foreach (SqlError sqlError in sqlException.Errors)
                {
                    if (sb.Length > 0) sb.Append(" ");
                    sb.Append(sqlError.Message);
                }

                return sb.ToString();
            }

            string error = "";

            if (!ex.Message.Contains("inner exception for details")) error += ex.Message;

            var iex = ex.InnerException;
            while (iex != null)
            {
                error += $";  {iex.Message}{(includeStacktrace ? "-----" + iex.StackTrace : "")}";
                iex = iex.InnerException;
            }

            return error;
        }
    }
}
