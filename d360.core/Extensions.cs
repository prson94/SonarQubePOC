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
using Newtonsoft.Json.Linq;
using d360.core.entities;
using Newtonsoft.Json;

namespace d360.core
{
    public static class JsonExtensions
    {
        public static int GetNumberOfDecimalPlaces(this decimal d)
        {
            int count = BitConverter.GetBytes(decimal.GetBits(d)[3])[2];
            return count;
        }
        public static List<FieldJsonProperty> ParseJsonIntoJsonPropertiesCollection(this string o)
        {
            var token = JToken.Parse(o);
            return token.ParseJsonIntoJsonPropertiesCollection();
        }

        public static List<FieldJsonProperty> ParseJsonIntoJsonPropertiesCollection(this JToken o)
        {
            List<FieldJsonProperty> properties = new List<FieldJsonProperty>();

            if (o is JArray)
            {
                properties = (o as JArray).ParseJsonIntoJsonPropertiesCollection();
            }
            else if (o is JObject)
            {
                properties = (o as JObject).ParseJsonIntoJsonPropertiesCollection(0);
            }

            return properties;
        }

        private static List<FieldJsonProperty> ParseJsonIntoJsonPropertiesCollection(this JArray o)
        {
            List<FieldJsonProperty> properties = new List<FieldJsonProperty>();

            int pos = 0;
            foreach (JToken c in o)
            {
                properties.AddRange(
                    (c as JObject).ParseJsonIntoJsonPropertiesCollection(pos)
                    );
                pos++;
            }

            return properties;
        }

        private static List<FieldJsonProperty> ParseJsonIntoJsonPropertiesCollection(this JObject o, int position = 0)
        {
            List<FieldJsonProperty> properties = new List<FieldJsonProperty>();

            foreach (JProperty p in o.Properties())
            {
                if (p.Value is JArray)
                {
                    properties.Add(new FieldJsonProperty { IsArray = true, Name = p.Name, Path = p.Path, Position = position });

                    int pos = 0;
                    foreach (JToken c in p.Value)
                    {
                        if (c is JObject)
                        {
                            properties.AddRange(
                                (c as JObject).ParseJsonIntoJsonPropertiesCollection(pos)
                                );
                        }
                        pos++;
                    }
                }
                else if (p.Value is JObject)
                {
                    properties.Add(new FieldJsonProperty { IsArray = false, Name = p.Name, Path = p.Path, Position = position });
                    properties.AddRange(
                        (p.Value as JObject).ParseJsonIntoJsonPropertiesCollection(position)
                        );
                }
                else
                {
                    properties.Add(new FieldJsonProperty
                    {
                        IsArray = false,
                        Name = p.Name,                        
                        Path = p.Path,
                        Position = position,
                        Value = p.Value.ToString()
                    });
                }
            }
        
            return properties;
        }
    }

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
                return Source;

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
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }
        /// <summary>
        /// Returns truncated string to 31 characters to accomodate xlsx sheet name limit
        /// </summary>
        public static string GetSafeSheetName(this string inputString)
        {
            if (inputString == null) return "";
            if (inputString.Length < 31)
                return inputString;

            return inputString.Substring(0, 28) + "...";
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

    }

    public static class GeneralExtensions
    {
        public static string GetFullExceptionData(this Exception ex, bool includeStacktrace = true, int characterLimit = -1)
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
                if (characterLimit == -1)
                {
                    return sb.ToString();
                }
                else
                {
                    string message = sb.ToString().Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, sb.Length));

                    return message;
                }
            }

            string error = "";

            if (!ex.Message.Contains("inner exception for details")) error += ex.Message;

            var iex = ex.InnerException;
            while (iex != null)
            {
                error += $";  {iex.Message}{(includeStacktrace ? "-----" + iex.StackTrace : "")}";
                iex = iex.InnerException;
            }

            if (characterLimit == -1)
            {
                return error;
            }
            else
            {
                string message = error.ToString().Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, error.Length));

                return message;
            }
        }

        public static string AsJson<T>(this T item)
        {
            var json = JsonConvert.SerializeObject(item);
            return json;
        }

        public static T CloneThis<T>(this T item)
        {
            var json = JsonConvert.SerializeObject(item);
            T newItem = JsonConvert.DeserializeObject<T>(json);
            return newItem;
        }
    }
}
