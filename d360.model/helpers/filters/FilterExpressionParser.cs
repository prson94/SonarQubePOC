using d360.core.entities;
using d360.model.helpers.filters;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace d360.model.helpers
{
    public class FilterExpressionParser
    {
        ICompanyContext CompanyContext;
        private List<FieldType> fieldTypes = new List<FieldType>();
        private List<string> fieldColumns = new List<string>();
        private List<int> filteredFieldIDs = new List<int>();
        private FilterExpressionParseType parseType;
        private List<DefaultFilter> allowedDefaultFields = new List<DefaultFilter>();
        private List<string> disallowedFieldTypes = new List<string>() { "ComplexRelationLookup", "", "OwnershipLookup", "RefListRelationship" };
        public List<FilterToken> FilterTokens = new List<FilterToken>();

        private bool registerTokensAsFields = false;

        public FilterExpressionParser(
            ICompanyContext ctx,
            FilterExpressionParseType type = FilterExpressionParseType.CustomFields,
            bool includeParent = false,
            bool useUserDefaultFields = false,
            bool registerTokensAsFields = false
            )
        {
            this.CompanyContext = ctx;
            this.parseType = type;
            this.registerTokensAsFields = registerTokensAsFields;
            allowedDefaultFields.Add(new DefaultFilter("Code", "A.Code", SqlFieldType.Text));
            allowedDefaultFields.Add(new DefaultFilter("Color", "JSON_VALUE((select top 1 * from dbo.GetAssetColorJsonByColor(A.Color)), '$.Name')", SqlFieldType.Text));
            allowedDefaultFields.Add(new DefaultFilter("[Path]", "KP.KeyPath", SqlFieldType.Text));
            allowedDefaultFields.Add(new DefaultFilter("[Level]", "LVL.Level", SqlFieldType.Number));
            allowedDefaultFields.Add(new DefaultFilter("uid", "A.Uid", SqlFieldType.Text));

            if (includeParent)
            {
                allowedDefaultFields.Add(new DefaultFilter("ParentDisplayName", "Parent.DisplayValue", SqlFieldType.Text));
                allowedDefaultFields.Add(new DefaultFilter("ParentUid", "Parent.Uid", SqlFieldType.Text));
            }

            allowedDefaultFields.Add(new DefaultFilter("CreatedOn", "A.CreatedOn", SqlFieldType.DateTime));
            allowedDefaultFields.Add(new DefaultFilter("UpdatedOn", "A.UpdatedOn", SqlFieldType.DateTime));

            if (useUserDefaultFields)
            {
                allowedDefaultFields.Clear();
                allowedDefaultFields.Add(new DefaultFilter("FirstName", "gr.FirstName", SqlFieldType.Text));
                allowedDefaultFields.Add(new DefaultFilter("LastName", "gr.LastName", SqlFieldType.Text));
                allowedDefaultFields.Add(new DefaultFilter("Email", "gr.Email", SqlFieldType.Text));
                allowedDefaultFields.Add(new DefaultFilter("IsAdministrator", "gr.IsAdministrator", SqlFieldType.Boolean));
                allowedDefaultFields.Add(new DefaultFilter("LastLoggedInOn", "gr.LastLoggedInOn", SqlFieldType.DateTime));
                allowedDefaultFields.Add(new DefaultFilter("CreatedOn", "gr.CreatedOn", SqlFieldType.DateTime));
                allowedDefaultFields.Add(new DefaultFilter("uid", "gr.uid", SqlFieldType.Text));
                allowedDefaultFields.Add(new DefaultFilter("State", @"(CASE gr.state 
                    WHEN 1 THEN 'Active'
                    WHEN 2 THEN 'InActive'
                    WHEN 3 THEN 'Deleted' END)", SqlFieldType.Text));

            }

            if (parseType == FilterExpressionParseType.CommunityResposibilityResource)
            {
                allowedDefaultFields.Clear();
                allowedDefaultFields.Add(new DefaultFilter("FirstName", "gr.FirstName + ' ' + gr.LastName", SqlFieldType.Text));
                allowedDefaultFields.Add(new DefaultFilter("OwnedItemCount", "OC.OwnedItemCount", SqlFieldType.Text));
                allowedDefaultFields.Add(new DefaultFilter("State", @"(CASE gr.state 
                    WHEN 1 THEN 'Active'
                    WHEN 2 THEN 'InActive'
                    WHEN 3 THEN 'Deleted' END)", SqlFieldType.Text));
            }

            //Rule results do not have field type db records, so we need to add fields manually
            if (parseType == FilterExpressionParseType.RuleResults)
            {
                allowedDefaultFields.Clear();
                allowedDefaultFields.Add(new DefaultFilter("EvaluatedAssetClass", "R.Class", SqlFieldType.AssetTypeClass));
                allowedDefaultFields.Add(new DefaultFilter("EvaluatedAssetTypePath", "R.EvaluatedAssetTypePath", SqlFieldType.Text));
                allowedDefaultFields.Add(new DefaultFilter("EvaluatedAssetPath", "R.Segments", SqlFieldType.Xml));
                allowedDefaultFields.Add(new DefaultFilter("EvaluatedAssetDisplayPath", "R.Segments", SqlFieldType.Xml));

                allowedDefaultFields.Add(new DefaultFilter("EffectiveDate", "R.EffectiveDate", SqlFieldType.Date));
                allowedDefaultFields.Add(new DefaultFilter("RunDate", "R.RunDate", SqlFieldType.DateTime));

                allowedDefaultFields.Add(new DefaultFilter("PassCount", "R.PassCount", SqlFieldType.Number));
                allowedDefaultFields.Add(new DefaultFilter("FailCount", "R.FailCount", SqlFieldType.Number));
                allowedDefaultFields.Add(new DefaultFilter("TotalCount", "R.TotalCount", SqlFieldType.Number));
                allowedDefaultFields.Add(new DefaultFilter("PassFraction", "R.PassFraction", SqlFieldType.Decimal));

                allowedDefaultFields.Add(new DefaultFilter("Outdated", "R.IsDuplicate", SqlFieldType.Boolean));
            }

            if (parseType == FilterExpressionParseType.RelationshipCustomFields)
            {
                allowedDefaultFields.Add(new DefaultFilter("State", @"(CASE I.State 
                    WHEN 1 THEN 'Active'
                    WHEN 2 THEN 'InActive'
                    WHEN 3 THEN 'Deleted' END)", SqlFieldType.Text));

                allowedDefaultFields.Add(new DefaultFilter("Object.[Path]", "ANDP_Object.DisplayPath", SqlFieldType.Text));
                allowedDefaultFields.Add(new DefaultFilter("Subject.[Path]", "ANDP_Subject.DisplayPath", SqlFieldType.Text));
            }
        }

        public void OverrideAllowedDefaultFields(List<DefaultFilter> defaultFilters)
        {
            allowedDefaultFields.Clear();
            allowedDefaultFields.AddRange(defaultFilters);
        }

        public void LoadFieldTypes(List<FieldType> fields, List<string> columns)
        {
            this.fieldTypes = fields;
            this.fieldColumns = columns;
        }

        public string ParseAsFiltersDataTable(string filterString)
        {
            try
            {
                if (parseType != FilterExpressionParseType.ComplexLookupField)
                {
                    throw new InvalidOperationException("ParseAsFiltersDataTable is only allowed for FilterExpressionParseType.ComplexLookupField");
                }

                Tokenize(filterString);

                StringBuilder query = new StringBuilder();

                foreach (var item in this.FilterTokens)
                {
                    query.Append(ParseTokensForComplexFields(item));
                }

                return query.Length > 0 ? $"({query})" : "";
            }
            catch (Exception ex)
            {
                throw new FilterExpressionParserException("Invalid filter expression: " + ex.Message);
            }
        }

        public string Parse(string filterString, out Dictionary<string, object> sqlParams, out List<int> fieldIds)
        {
            if (parseType == FilterExpressionParseType.ComplexLookupField)
            {
                throw new InvalidOperationException("For FilterExpressionParseType.ComplexLookupField call ParseAsFiltersDataTable");
            }
            fieldIds = this.filteredFieldIDs;
            this.FilterTokens.Clear();
            try
            {
                return GetSQL(filterString.Trim(), out sqlParams);
            }
            catch (IndexOutOfRangeException)
            {
                throw new FilterExpressionParserException("Invalid filter expression: ", new Exception("One or more filter expressions has missing operator or value."));
            }
            catch (Exception ex)
            {
                throw new FilterExpressionParserException("Invalid filter expression: " + ex.Message);
            }
        }

        private string GetSQL(string filterString, out Dictionary<string, object> sqlParams)
        {
            sqlParams = new Dictionary<string, object>();
            if (string.IsNullOrEmpty(filterString))
            {
                return "";
            }

            filterString = filterString.Trim();

            StringBuilder sb = new StringBuilder();

            Tokenize(filterString);

            if (parseType == FilterExpressionParseType.Relationships)
            {
                CheckRelationshipTokens(FilterTokens);
            }

            foreach (var token in FilterTokens)
            {
                if (parseType == FilterExpressionParseType.CustomFields || parseType == FilterExpressionParseType.RuleResults || parseType == FilterExpressionParseType.RelationshipCustomFields || parseType == FilterExpressionParseType.CommunityResposibilityResource)
                {
                    ParseTokensForCustomFields(sqlParams, sb, token);
                }
                else if (parseType == FilterExpressionParseType.Relationships)
                {
                    ParseTokensForRelationships(sqlParams, sb, token);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            return sb.ToString();
        }

        private void Tokenize(string filterString)
        {
            Regex regex = new Regex(@"\'(.+?)\'");
            var matchGroups = regex.Matches(filterString);

            List<Tuple<string, string>> valuesMap = new List<Tuple<string, string>>();
            for (int j = 0; j < matchGroups.Count; j++)
            {
                var key = "#valueToken" + Guid.NewGuid();
                var matchValue = matchGroups[j].Value;
                filterString = filterString.Replace(matchValue, key.ToLower());
                valuesMap.Add(new Tuple<string, string>(key, matchValue));
            }


            if (!ValidateString(filterString))
            {
                throw new FormatException("Filter expression contains unclosed quotations or brackets.");
            }

            string[] tokens = GetTokens(ref filterString);

            for (int j = 0; j < tokens.Length; j++)
            {
                if (valuesMap.Any(x => x.Item1.ToLower() == tokens[j].ToLower()))
                {
                    var value = valuesMap.FirstOrDefault(x => x.Item1.ToLower() == tokens[j].ToLower()).Item2;
                    tokens[j] = value;
                }
            }

            bool expectingCondition = false;
            int paramCount = 0;
            int i = 0;
            while (i < tokens.Length)
            {
                if (tokens[i] == "(")
                {
                    FilterTokens.Add(new FilterToken(CompanyContext, null, "(", null));
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
        }

        private void ParseTokensForRelationships(Dictionary<string, object> sqlParams, StringBuilder sb, FilterToken token)
        {
            if (token.IsOnlyOperator)
            {
                sb.Append(token.GetSQLForOperator());
            }
            else if (token.IsNullValue)
            {
                sb.Append(token.GetSQLForRelationshipNull(sqlParams));
            }
            else
            {
                sb.Append(token.GetSQLForRelationship(sqlParams));
            }
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

                if (fieldType != null && disallowedFieldTypes.Contains(fieldType.Type))
                {
                    throw new Exception("Field with name '" + token.Field + "' is not supported (" + fieldType.Type + ")!");
                }

                if (fieldType == null)
                {
                    if (allowedDefaultFields.Any(x => x.ApiName.ToLower() == token.Field.ToLower()))
                    {
                        var val = allowedDefaultFields.FirstOrDefault(x => x.ApiName.ToLower() == token.Field.ToLower());
                        sb.Append(token.GetSQLForDefaultField(sqlParams, val));
                    }
                    else if (this.registerTokensAsFields == true)
                    {
                        var val = new DefaultFilter(token.Field, token.Field, SqlFieldType.Text);
                        sb.Append(token.GetSQLForDefaultField(sqlParams, val));

                    }
                    else if (token.IsOwnerFilter)
                    {
                        sb.Append(token.GetSQLForOwnerField(sqlParams));
                    }
                    else if (token.IsRlationshipFilter)
                    {
                        sb.Append(token.GetSQLForRelationField(sqlParams));
                    }
                    else
                    {
                        throw new Exception("Field with name '" + token.Field + "' does not exist!");
                    }
                }
                else
                {
                    this.filteredFieldIDs.Add(fieldType.ID);

                    token.LoadFieldType(fieldType, fieldColumns);
                    sb.Append(token.GetSQLForField(sqlParams));
                }
            }
        }

        private string ParseTokensForComplexFields(FilterToken token)
        {
            token.IsComplexField = true;
            if (token.IsOnlyOperator)
            {
                return token.@operator;
            }
            if (token.Field.ToLower().StartsWith("$related:"))
            {
                return GetRelationshipsSQLForComplexField(token);
            }
            else
            {
                var fieldType = this.fieldTypes.FirstOrDefault(x => x.Name.ToLower() == token.Field);

                if (fieldType != null && disallowedFieldTypes.Contains(fieldType.Type))
                {
                    throw new Exception("Field with name '" + token.Field + "' is not supported (" + fieldType.Type + ")!");
                }

                token.LoadFieldType(fieldType, null);

                if (!token.IsNullValue)
                {
                    token.UpdateTokenValueForType();
                    return $"( {token.Field} {token.GetSQLOperator(token.@operator)} '{token.EscapedValueAsString}')";
                }
                else
                {
                    if (!(new[] { "eq", "ne" }.Contains(token.@operator)))
                    {
                        throw new FormatException($"NULL value filter can be used only with 'eq' and 'ne' operator!");
                    }

                    return $"( {token.Field} { token.GetSQLNullOperator(token.@operator)})";
                }

            }
        }

        private string GetRelationshipsSQLForComplexField(FilterToken token)
        {
            var intersectTypeUid = token.Field.ToLower().Replace("$related:", "");
            var intersectUid = token.EscapedValueAsString;
            var ftRelationship = fieldTypes.Where(x => x.Name.ToLower() == token.Field.ToLower()).FirstOrDefault();
            var ftQueryName = fieldTypes.FirstOrDefault(x => x.LookupObjectID == ftRelationship.LookupObjectID && x.LookupObjectType == ftRelationship.LookupObjectType && ftRelationship.Name != x.Name).Name;
            var relationshipFilterSQL = "";
            if (ftRelationship != null)
            {
                string sqlOperator = "=";
                string relField = ftQueryName.Replace("_IntersectTypeUid", "_Uid");
                if (token.IsNullValue)
                {
                    sqlOperator = " is null ";
                    if (token.@operator == "ne")
                    {
                        sqlOperator = " is not null";
                    }

                    relationshipFilterSQL = $"( {ftQueryName} {sqlOperator})";
                }
                else
                {
                    if (token.@operator == "ne")
                    {
                        sqlOperator = "<>";
                    }

                    relationshipFilterSQL = $"( {ftQueryName} = '{intersectTypeUid}' and {relField} {sqlOperator} '{intersectUid.Replace("'", "")}')";
                }

            }

            return relationshipFilterSQL;
        }

        private string[] GetTokens(ref string filterString)
        {
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

            filterString = filterString.Replace("(", " ( ").Replace(")", " ) ");

            return filterString.Split(' ').Select(x => x.Trim().Replace("&nbsp;", " ").ToLower()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
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

        private void CheckRelationshipTokens(List<FilterToken> tokens)
        {
            List<Guid> IntersectUids = new List<Guid>();
            List<Guid> AssetUids = new List<Guid>();
            foreach (var token in tokens.Where(x => x.IsOnlyOperator == false && x.IsNullValue != true))
            {
                var intersectUid = Guid.Empty;
                var assetUid = Guid.Empty;

                if (!Guid.TryParse(token.Field, out intersectUid))
                {
                    throw new Exception($"Invalid Relationship Type UID Provided ({token.Field}).");
                }

                if (!Guid.TryParse(token.ValueAsString, out assetUid))
                {
                    throw new Exception($"Invalid Asset UID Provided ({token.ValueAsString}).");
                }

                IntersectUids.Add(intersectUid);
                AssetUids.Add(assetUid);
            }

            var intersectTypes = CompanyContext.IntersectTypes.Where(x => IntersectUids.Contains(x.uid)).ToList();
            var filterAssets = CompanyContext.Assets.Where(x => AssetUids.Contains(x.uid)).Include(x => x.AssetType).ToList();
            var filterAssetTypes = CompanyContext.AssetTypes.Where(x => AssetUids.Contains(x.uid)).ToList();

            foreach (var itUid in IntersectUids)
            {
                if (!intersectTypes.Any(x => x.uid == itUid))
                {
                    throw new Exception($"Relationship Type with UID '{itUid.ToString()}' does not exist.");
                }
            }

            foreach (var assetUid in AssetUids)
            {
                if (!filterAssets.Any(x => x.uid == assetUid) && !filterAssetTypes.Any(x => x.uid == assetUid))
                {
                    throw new Exception($"Asset with UID '{assetUid.ToString()}' does not exist.");
                }
            }

            //Load data to tokens
            foreach (var token in tokens.Where(x => x.IsOnlyOperator == false))
            {
                var intersectUid = Guid.Empty;
                var assetUid = Guid.Empty;

                Guid.TryParse(token.Field, out intersectUid);
                Guid.TryParse(token.ValueAsString, out assetUid);

                token.LoadRelationshipData(
                    intersectTypes.FirstOrDefault(x => x.uid == intersectUid),
                    filterAssets.FirstOrDefault(x => x.uid == assetUid)?.AssetType ?? filterAssetTypes.FirstOrDefault(x => x.uid == assetUid));


            }

        }
    }

    public enum FilterExpressionParseType
    {
        CustomFields,
        Relationships,
        RuleResults,
        RelationshipCustomFields,
        CommunityResposibilityResource,
        ComplexLookupField
    }

    public enum SqlFieldType
    {
        Text, Boolean, Number, Decimal, Date, DateTime, Guid, AssetTypeClass, Xml
    }

    public class DefaultFilter
    {
        public string ApiName { get; set; }
        public string SqlExpression { get; set; }
        public SqlFieldType SqlFieldType { get; set; }

        public DefaultFilter(string apiName, string sqlExpression, SqlFieldType sqlFieldType)
        {
            this.ApiName = apiName;
            this.SqlExpression = sqlExpression;
            this.SqlFieldType = sqlFieldType;
        }
    }
}
