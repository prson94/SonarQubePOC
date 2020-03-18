using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace d360.model.helpers
{
    public enum FilterExpressionParseType
    {
        CustomFields,
        Relationships
    }
    public class FilterExpressionParser
    {
        ICompanyContext CompanyContext;
        private List<FieldType> fieldTypes = new List<FieldType>();
        private List<string> fieldColumns = new List<string>();
        private FilterExpressionParseType parseType;
        private List<Tuple<string, string>> allowedDefaultFields = new List<Tuple<string, string>>();

        public FilterExpressionParser(
            ICompanyContext ctx,
            List<FieldType> fields,
            List<string> columns,
            FilterExpressionParseType type = FilterExpressionParseType.CustomFields,
            bool includeParent = false)
        {
            this.CompanyContext = ctx;
            this.fieldTypes = fields;
            this.fieldColumns = columns;
            this.parseType = type;

            allowedDefaultFields.Add(new Tuple<string, string>("Code", "Code"));

            if (includeParent)
            {
                allowedDefaultFields.Add(new Tuple<string, string>("ParentDisplayName", "Parent.DisplayValue"));
            }
        }

        public string Parse(string filterString, out Dictionary<string, object> sqlParams)
        {
            try
            {
                return GetSQL(filterString.Trim(), out sqlParams);
            }
            catch (Exception ex)
            {
                throw new Exception("Invalid filter expression: ", ex);
            }
        }

        private string GetSQL(string filterString, out Dictionary<string, object> sqlParams)
        {
            sqlParams = new Dictionary<string, object>();
            if (string.IsNullOrEmpty(filterString))
            {
                return "";
            }

            if (!ValidateString(filterString))
            {
                throw new FormatException("Filter expression contains unclosed quotations or brackets.");
            }

            string[] tokens = GetTokens(ref filterString);
            StringBuilder sb = new StringBuilder();

            List<FilterToken> FilterTokens = new List<FilterToken>();

            bool expectingCondition = false;
            int paramCount = 0;
            int i = 0;
            while (i < tokens.Length)
            {
                if (tokens[i] == "(")
                {
                    FilterTokens.Add(new FilterToken(this.CompanyContext, null, "(", null));
                    i++;
                    continue;
                }
                if (tokens[i] == ")")
                {
                    FilterTokens.Add(new FilterToken(this.CompanyContext, null, ")", null));
                    i++;
                    continue;
                }

                if (!expectingCondition)
                {
                    paramCount++;
                    FilterTokens.Add(new FilterToken(this.CompanyContext, tokens[i], tokens[i + 1], tokens[i + 2], paramCount));
                    expectingCondition = true;
                    i += 3;
                    continue;
                }

                if (expectingCondition)
                {
                    FilterTokens.Add(new FilterToken(this.CompanyContext, null, tokens[i], null));
                    expectingCondition = false;
                    i++;
                    continue;
                }
                i = tokens.Length;
            }


            foreach (var token in FilterTokens)
            {
                if (parseType == FilterExpressionParseType.CustomFields)
                {
                    ParseTokensForCustomFields(sqlParams, sb, token);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            return sb.ToString();
        }

        private void ParseTokensForCustomFields(Dictionary<string, object> sqlParams, StringBuilder sb, FilterToken token)
        {
            if (token.IsOnlyOperator)
            {
                sb.Append(token.GetSQLForOperator());
            }
            else
            {
                var fieldType = this.fieldTypes.FirstOrDefault(x => x.Name.ToLower() == token.Field);
                if (fieldType == null)
                {
                    if (allowedDefaultFields.Any(x => x.Item1.ToLower() == token.Field.ToLower()))
                    {
                        var val = allowedDefaultFields.FirstOrDefault(x => x.Item1.ToLower() == token.Field.ToLower());
                        sb.Append(token.GetSQLForDefaultField(ref sqlParams, val.Item2));
                    }
                    else
                    {
                        throw new Exception("Field with name '" + token.Field + "' does not exist!");
                    }
                }
                else
                {
                    token.LoadFieldType(fieldType, fieldColumns);
                    sb.Append(token.GetSQLForField(ref sqlParams));
                }
            }
        }

        private string[] GetTokens(ref string filterString)
        {
            filterString = filterString.Trim();
            var replaceIndexes = GetAllIndexesOf('\'', filterString);
            var length = filterString.Length;
            for (int i = 0; i < replaceIndexes.Length; i += 2)
            {

                var subString = filterString.Substring(replaceIndexes[i], replaceIndexes[i + 1] - replaceIndexes[i]);
                filterString = filterString.Replace(subString, subString.Replace(" ", "&nbsp;"));
                int diff = filterString.Length - length;
                for (int j = i + 1; j < replaceIndexes.Length; j++)
                {
                    replaceIndexes[j] += diff;
                }
                length = filterString.Length;

            }

            return filterString.Replace("(", " ( ").Replace(")", " ) ").Split(' ').Select(x => x.Trim().Replace("&nbsp;", " ").ToLower()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
        }

        private bool ValidateString(string str)
        {
            int bracketCount = 0;
            int apostropheCount = 0;

            foreach (char c in str)
            {
                if (c == '(') bracketCount++;
                if (c == ')') bracketCount--;
                if (c == '\'') apostropheCount++;
            }

            return bracketCount == 0 && apostropheCount % 2 == 0;

        }
        private int[] GetAllIndexesOf(char c, string s)
        {
            List<int> indx = new List<int>();
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == c) indx.Add(i);
            }
            return indx.ToArray();
        }

    }
}
