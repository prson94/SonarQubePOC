using System;
using System.Linq;

namespace d360.model.helpers.filters.program
{
    public class FilterHelpers
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
                throw new Exception("Text values should be placed within quotations.");
            }
        }
    }
}
