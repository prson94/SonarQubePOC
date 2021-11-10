using System;
using System.Data.SqlClient;
using Newtonsoft.Json;

namespace d360.core
{
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
                    string message = sb.ToString().Substring(0, Math.Min(characterLimit, sb.Length));

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
                string message = error.Substring(0, Math.Min(characterLimit, error.Length));

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