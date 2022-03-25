using System;
using System.Linq;
using System.Text;

namespace d360.model.helpers.filters.program
{
    public static class FilterHelpers
    {
        public static bool IsValidOperatorForFieldType(string fieldType, string operand)
        {
            switch (fieldType)
            {
                case "boolean":
                case "lookup":
                case "relationship":
                    return new[] { "eq", "ne", "ct" }.Contains(operand);
                case "number":
                case "decimal":
                case "score":
                case "counter":
                    return !(new[] { "ct", "nct" }.Contains(operand));
                case "date":
                case "datetime":
                    return new[] { "ct", "nct", "eq", "ne", "gt", "ge", "lt", "le" }.Contains(operand);
                case "assettypeclass":
                    return new[] { "eq", "ne" }.Contains(operand);
                default:
                    return new[] { "eq", "ne", "ct", "nct" }.Contains(operand);
            }
        }

        public static void ValidateValueForType(string type, object value)
        {
            bool hasApostrophe = value.ToString().First() == '\'' && value.ToString().Last() == '\'';

            if (!hasApostrophe && !(type == "number" || type == "decimal" || type == "boolean" || type == "score" || type == "counter"))
            {
                throw new FilterExpressionParserException("Text values should be placed within quotations.");
            }
        }

        public static string GetSQLOperator(string value)
        {
            switch (value)
            {
                case "eq": return " = ";
                case "ne": return " <> ";
                case "gt": return " > ";
                case "ge": return " >= ";
                case "lt": return " < ";
                case "le": return " <= ";
                case "ct": return " like ";
                case "nct": return " not like ";
                default: throw new FilterExpressionParserException($"Invalid comparison operator '{value}'");
            }
        }

        public static string GetSQLNullOperator(string value)
        {
            switch (value)
            {
                case "eq": return " is null";
                case "ne": return " is not null";
                default: throw new FilterExpressionParserException($"Invalid comparison operator '{value}'");
            }
        }

        public static string WildcardValue(string value)
        {
            return string.IsNullOrEmpty(value) ? "" : value.Replace("*", "%").Replace("?", "_");
        }

        public static string EscapeForSQLLike(string value)
        {
            char[] escapeChars = new[] { '%', '_', '^', '[' };
            StringBuilder escapedValue = new StringBuilder();

            foreach (char c in value)
            {
                if (escapeChars.Contains(c))
                {
                    escapedValue.Append($"[{c}]");
                }
                else
                {
                    escapedValue.Append(c);
                }
            }

            return escapedValue.ToString();
        }

        public static string GetLogicalOperator(string value)
        {
            switch (value)
            {
                case "and": return " and ";
                case "or": return " or ";
                default: throw new FilterExpressionParserException($"Invalid logical operator '{value}'");
            }
        }
    }
}
