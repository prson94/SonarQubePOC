using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Data;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Xml;
using System.Diagnostics.Contracts;
using d360.core.entities;
using d360.core.resources;

namespace d360.core
{
    public static class DateTimeExtensions
    {
        public static long Epoch(this DateTime d)
        {
            return (long)(d.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalSeconds;
        }
        public static DateTime FromEpoch(int epoch)
        {

            DateTime d = new DateTime(1970, 1, 1);
            d = d.AddSeconds(epoch);
            return d;

        }
        public static long UtcNowEpoch()
        {
            return DateTime.UtcNow.Epoch();
        }
    }

    public static class StringExtensions
    {
        public static string FormatBooleanReadOnlyValue(this bool b)
        {
            return b ? Values.BooleanTrue : Values.BooleanFalse;
        }

        public static string CleanColumnName(this string s)
        {
            s = s.Replace(" ", "-");
            s = s.Replace("$", "_");
            s = System.Text.RegularExpressions.Regex.Replace(s, "[0123456789]", "__");
            return s;
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

        public static string HierarchyPathToText(this string xml)
        {
            Contract.Requires(!string.IsNullOrEmpty(xml));

            string separator = " // ";
            string html = "";
            var doc = XElement.Parse(xml);

            var lastNodeID = doc.Elements().Last().Attribute("id").Value;

            foreach (XElement node in doc.Elements())
            {
                html += node.Attribute("name").Value;
                if (node.Attribute("id").Value != lastNodeID)
                    html += separator;
            }

            //html = html.Remove(html.LastIndexOf(separator) + 1);

            return html;
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
        
        /// <returns>The previously tokenized string with its replaced field values.</returns>
        public static string ReplaceTokenWithValues(this string tokenFormatString, List<Field> fields)
        {
            List<string> tokens = tokenFormatString.ParseTokens();
            return tokenFormatString.ReplaceTokenWithValues(tokens, fields);
        }

        /// <summary>
        /// Replaces field token with their actual values from system lookups.
        /// </summary>
        /// <param name="xml">The list of tokens pulled from the tokenized string.</param>
        /// <param name="xml">The XML that contains all the field names and values.</param>
        /// <returns>The previously tokenized string with its replaced field values.</returns>
        public static string ReplaceTokenWithValues(this string tokenFormatString, List<string> tokens, List<Field> fields)
        {
            // Format the text based on the tokens.
            foreach (string token in tokens)
            {
                var n = token.Substring(1, token.Length - 2);   // Name of the element to find in the XML.
                var fld = fields.FirstOrDefault(t => t.FieldType.Name == n);
                if (fld != null)
                    tokenFormatString = tokenFormatString.Replace(token, fld.Value);
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

        public static XElement ToXElement(this string xml)
        {
            return XElement.Parse(xml);
        }
    }

    public static class GeneralExtensions
    {
        public static string GetFullExceptionData(this Exception ex)
        {
            string error = "";

            error += ex.Message;
            var iex = ex.InnerException;
            while (iex != null)
            {
                error += ";  " + iex.Message + "-----" + iex.StackTrace;
                iex = iex.InnerException;
            }

            return error;
        }

        public static string FormatDisplayName(this Resource r)
        {
            try
            {
                return string.Format("{0} {1}", r.FirstName, r.LastName);
            }
            catch
            {
                return "Unable to resolve resource name";
            }
        }
    }
}
