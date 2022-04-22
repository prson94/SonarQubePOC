using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using d360.core.entities;
using d360.core.enums;
using d360.model.helpers.filters;

namespace d360.model.helpers
{
    public class FilterExpressionParser
    {
        private readonly IFilterDataProvider dataProvider;
        private List<FieldType> fieldTypes = new List<FieldType>();
        private List<string> fieldColumns = new List<string>();
        private readonly List<int> filteredFieldIDs = new List<int>();
        private readonly FilterExpressionParseType parseType;
        private readonly List<DefaultFilter> allowedDefaultFields = new List<DefaultFilter>();
        private readonly List<string> disallowedFieldTypes = new List<string> { "ComplexRelationLookup", "", "OwnershipLookup", "RefListRelationship" };

        private readonly bool registerTokensAsFields;

        public FilterExpressionParser(
            IFilterDataProvider fdp,
            FilterExpressionParseType type = FilterExpressionParseType.CustomFields,
            bool includeParent = false,
            bool useUserDefaultFields = false,
            bool registerTokensAsFields = false
        )
        {
            dataProvider = fdp;
            parseType = type;
            this.registerTokensAsFields = registerTokensAsFields;
            allowedDefaultFields.Add(new DefaultFilter("Code", "A.Code", SqlFieldType.Text));
            allowedDefaultFields.Add(new DefaultFilter("Color", "JSON_VALUE((select top 1 * from dbo.GetAssetColorJsonByColor(A.Color)), '$.Name')", SqlFieldType.Text));
            allowedDefaultFields.Add(new DefaultFilter("[Path]", "Node.KeyPath", SqlFieldType.Text));
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
                allowedDefaultFields.Add(new DefaultFilter("OwnedItemCount", "OC.OwnedItemCount", SqlFieldType.Number));
                allowedDefaultFields.Add(new DefaultFilter("State", @"(CASE gr.state 
                    WHEN 1 THEN 'Active'
                    WHEN 2 THEN 'InActive'
                    WHEN 3 THEN 'Deleted' END)", SqlFieldType.Text));
            }

            //Rule results do not have field type db records, so we need to add fields manually
            if (parseType == FilterExpressionParseType.RuleResults)
            {
                allowedDefaultFields.Clear();
                allowedDefaultFields.Add(new DefaultFilter("EvaluatedAssetClass", "E.Class", SqlFieldType.AssetTypeClass));
                allowedDefaultFields.Add(new DefaultFilter("EvaluatedAssetTypePath", "P.Path", SqlFieldType.Text));
                allowedDefaultFields.Add(new DefaultFilter("EvaluatedAssetPath", "E.Segments", SqlFieldType.Xml));
                allowedDefaultFields.Add(new DefaultFilter("EvaluatedAssetDisplayPath", "E.Segments", SqlFieldType.Xml));

                allowedDefaultFields.Add(new DefaultFilter("EffectiveDate", "R.EffectiveDate", SqlFieldType.Date));
                allowedDefaultFields.Add(new DefaultFilter("RunDate", "R.RunDate", SqlFieldType.DateTime));

                allowedDefaultFields.Add(new DefaultFilter("PassCount", "R.PassCount", SqlFieldType.Number));
                allowedDefaultFields.Add(new DefaultFilter("FailCount", "R.FailCount", SqlFieldType.Number));
                allowedDefaultFields.Add(new DefaultFilter("TotalCount", "R.TotalCount", SqlFieldType.Number));
                allowedDefaultFields.Add(new DefaultFilter("PassFraction", "R.PassFraction", SqlFieldType.Decimal));

                allowedDefaultFields.Add(new DefaultFilter("Outdated", "coalesce(E.IsDuplicate, R.IsDuplicate)", SqlFieldType.Boolean));
            }

            if (parseType == FilterExpressionParseType.RelationshipCustomFields)
            {
                allowedDefaultFields.Add(new DefaultFilter("State", @"(CASE I.State 
                    WHEN 1 THEN 'Active'
                    WHEN 2 THEN 'InActive'
                    WHEN 3 THEN 'Deleted' END)", SqlFieldType.Text));

                allowedDefaultFields.Add(new DefaultFilter("Object.[Path]", "ANDP_Object.DisplayPath", SqlFieldType.Text));
                allowedDefaultFields.Add(new DefaultFilter("Subject.[Path]", "ANDP_Subject.DisplayPath", SqlFieldType.Text));
                allowedDefaultFields.Add(new DefaultFilter("relationshiptype", "T.Uid", SqlFieldType.Guid));
                allowedDefaultFields.Add(new DefaultFilter("assetpath", "RelationshipSideData.AssetPath", SqlFieldType.Text));
            }

            if (parseType == FilterExpressionParseType.Semantics)
            {
                allowedDefaultFields.Clear();
                allowedDefaultFields.Add(new DefaultFilter("uid", "Uid", SqlFieldType.Text));
                allowedDefaultFields.Add(new DefaultFilter("name", "Name", SqlFieldType.Text));
                allowedDefaultFields.Add(new DefaultFilter("description", "Description", SqlFieldType.Text));
                allowedDefaultFields.Add(new DefaultFilter("qualifier", "Qualifier", SqlFieldType.Text));
                allowedDefaultFields.Add(new DefaultFilter("status",
                    SemanticStatus.Draft.GetSqlCaseFilterStatement("Status"), SqlFieldType.Text));
                allowedDefaultFields.Add(new DefaultFilter("source",
                    SemanticSource.BuiltIn.GetSqlCaseFilterStatement("[Source]"), SqlFieldType.Text));
                allowedDefaultFields.Add(new DefaultFilter("matchtype",
                    SemanticMatchType.Advanced.GetSqlCaseFilterStatement("MatchType"), SqlFieldType.Text));
                allowedDefaultFields.Add(new DefaultFilter("threshold", "Threshold", SqlFieldType.Number));
                allowedDefaultFields.Add(new DefaultFilter("priority", "Priority", SqlFieldType.Number));
                allowedDefaultFields.Add(new DefaultFilter("baseType",
                    SemanticBaseType.Boolean.GetSqlCaseFilterStatement("BaseType"), SqlFieldType.Text));
                allowedDefaultFields.Add(new DefaultFilter("effectiveDate", "EffectiveDate", SqlFieldType.DateTime));
                allowedDefaultFields.Add(new DefaultFilter("createdOn", "CreatedOn", SqlFieldType.DateTime));
                allowedDefaultFields.Add(new DefaultFilter("updatedOn", "UpdatedOn", SqlFieldType.DateTime));
                allowedDefaultFields.Add(new DefaultFilter("createdBy", "CreatedBy", SqlFieldType.Text));
                allowedDefaultFields.Add(new DefaultFilter("updatedBy", "UpdatedBy", SqlFieldType.Text));
            }

            if (parseType == FilterExpressionParseType.Tags)
            {
                allowedDefaultFields.Clear();
                allowedDefaultFields.Add(new DefaultFilter("value", "t.Value", SqlFieldType.Text));
                allowedDefaultFields.Add(new DefaultFilter("useCount", "Tags.count", SqlFieldType.Number));
                allowedDefaultFields.Add(new DefaultFilter("createdOn", "t.CreatedOn", SqlFieldType.DateTime));
                allowedDefaultFields.Add(new DefaultFilter("createdBy", "grc.FirstName + ' ' +grc.LastName", SqlFieldType.Text));
            }
        }

        public void OverrideAllowedDefaultFields(List<DefaultFilter> defaultFilters)
        {
            allowedDefaultFields.Clear();
            allowedDefaultFields.AddRange(defaultFilters);
        }

        public void LoadFieldTypes(List<FieldType> fields, List<string> columns)
        {
            fieldTypes = fields;
            fieldColumns = columns;
        }

        public string Parse(string filterString, out Dictionary<string, object> sqlParams, out List<int> fieldIds)
        {
            try
            {
                fieldIds = filteredFieldIDs;

                sqlParams = new Dictionary<string, object>();
                if (string.IsNullOrEmpty(filterString))
                {
                    return "";
                }

                filterString = filterString.Trim();

                StringBuilder sb = new StringBuilder();

                List<IFilterToken> filterTokens = Tokenize(filterString);

                LoadRelationshipDataForTokens(filterTokens);

                foreach (IFilterToken token in filterTokens)
                {
                    sb.Append($"{token.GetSqlExpression(sqlParams)}");
                }

                return sb.ToString();
            }
            catch (IndexOutOfRangeException)
            {
                throw new FilterExpressionParserException("Invalid filter expression: ", new Exception("One or more filter expressions has missing operator or value."));
            }
            catch (FilterExpressionParserException ex)
            {
                throw new FilterExpressionParserException("Invalid filter expression: " + ex.Message);
            }
            catch (Exception ex)
            {
                throw new FilterExpressionParserException("Invalid filter expression: " + ex.Message);
            }
        }

        private List<IFilterToken> Tokenize(string filterString)
        {
            List<IFilterToken> ret = new List<IFilterToken>();
            Regex regex = new Regex(@"\'(.+?)\'");
            MatchCollection matchGroups = regex.Matches(filterString);

            List<Tuple<string, string>> valuesMap = new List<Tuple<string, string>>();
            for (int j = 0; j < matchGroups.Count; j++)
            {
                string key = "#valueToken" + Guid.NewGuid();
                string matchValue = matchGroups[j].Value;
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
                    string value = valuesMap.FirstOrDefault(x => x.Item1.ToLower() == tokens[j].ToLower()).Item2;
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
                    ret.Add(new OperatorToken(dataProvider, null, "(", null));
                    i++;
                    continue;
                }

                if (tokens[i] == ")")
                {
                    ret.Add(new OperatorToken(dataProvider, null, ")", null));
                    i++;
                    continue;
                }

                if (!expectingCondition)
                {
                    paramCount++;
                    if (parseType == FilterExpressionParseType.Relationships)
                    {
                        ret.Add(new RelationshipFieldToken(dataProvider, tokens[i], tokens[i + 1], tokens[i + 2], paramCount));
                    }
                    else
                    {
                        ret.Add(GetFilterForTokens(dataProvider, tokens[i], tokens[i + 1], tokens[i + 2], paramCount));
                    }

                    expectingCondition = true;
                    i += 3;

                    continue;
                }

                if (expectingCondition)
                {
                    ret.Add(new OperatorToken(dataProvider, null, tokens[i], null));
                    expectingCondition = false;
                    i++;

                    continue;
                }

                i = tokens.Length;
            }

            return ret;
        }

        private IFilterToken GetFilterForTokens(IFilterDataProvider fdp, string field, string op, object value, int? paramIdx = null)
        {
            string fieldName = field.ToLower(System.Globalization.CultureInfo.InvariantCulture);
            FieldType fieldType = fieldTypes.FirstOrDefault(x => x.Name.ToLower() == fieldName);

            if (fieldType != null && disallowedFieldTypes.Contains(fieldType.Type))
            {
                throw new FilterExpressionParserException("Field with name '" + fieldName + "' is not supported (" + fieldType.Type + ")!");
            }

            if (fieldType == null)
            {
                if (allowedDefaultFields.Any(x => x.ApiName.ToLowerInvariant() == fieldName.ToLowerInvariant()))
                {
                    DefaultFilter val = allowedDefaultFields.FirstOrDefault(x => x.ApiName.ToLowerInvariant() == fieldName.ToLowerInvariant());
                    
                    return new DefaultFieldToken(fdp, field, op, value, val, paramIdx);
                }
                else if (registerTokensAsFields == true)
                {
                    DefaultFilter val = new DefaultFilter(fieldName, fieldName, SqlFieldType.Text);
                    return new DefaultFieldToken(fdp, field, op, value, val, paramIdx);
                }
                else if (fieldName.StartsWith("$ownedbyandresponsibility"))
                {
                    return new OwnerAndResponsibilityFieldToken(fdp, field, op, value, paramIdx);
                }
                else if (fieldName.StartsWith("$owned"))
                {
                    return new OwnerFieldToken(fdp, field, op, value, paramIdx);
                }
                else if (fieldName.StartsWith("$related"))
                {
                    return new RelationshipFieldToken(fdp, field, op, value, paramIdx);
                }
                else
                {
                    throw new FilterExpressionParserException("Field with name '" + fieldName + "' does not exist!");
                }
            }
            else
            {
                filteredFieldIDs.Add(fieldType.ID);
                if (parseType == FilterExpressionParseType.ComplexLookupField)
                {
                    if (fieldName.StartsWith("$related"))
                    {
                        return new RelationshipComplexFieldToken(fdp, field, op, value, fieldTypes);
                    }

                    ComplexFieldToken token = new ComplexFieldToken(fdp, field, op, value, paramIdx);
                    token.LoadFieldType(fieldType, fieldColumns);
                    
                    return token;
                }
                else
                {
                    FieldToken token = new FieldToken(fdp, field, op, value, paramIdx);
                    token.LoadFieldType(fieldType, fieldColumns);
                    
                    return token;
                }
            }
        }

        private string[] GetTokens(ref string filterString)
        {
            int[] replaceIndexes = GetAllIndexesOf('\'', filterString);
            int length = filterString.Length;
            for (int i = 0; i < replaceIndexes.Length; i += 2)
            {
                string subString = filterString.Substring(replaceIndexes[i], replaceIndexes[i + 1] - replaceIndexes[i]);
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
                if (c == '(')
                {
                    bracketCount++;
                }

                if (c == ')')
                {
                    bracketCount--;
                }

                if (c == '\'')
                {
                    apostropheCount++;
                }
            }

            return bracketCount == 0 && apostropheCount % 2 == 0;
        }

        private int[] GetAllIndexesOf(char c, string s)
        {
            List<int> indx = new List<int>();
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == c)
                {
                    indx.Add(i);
                }
            }

            return indx.ToArray();
        }

        private void LoadRelationshipDataForTokens(List<IFilterToken> tokens)
        {
            List<IFilterToken> relationshipTokens = tokens.Where(x => x is RelationshipFieldToken).ToList();
            if (relationshipTokens.Count == 0)
            {
                return;
            }

            List<Guid> IntersectUids = new List<Guid>();
            List<Guid> AssetUids = new List<Guid>();

            foreach (RelationshipFieldToken token in relationshipTokens)
            {
                Guid intersectUid = Guid.Empty;
                Guid assetUid = Guid.Empty;

                if (!Guid.TryParse(token.Field, out intersectUid))
                {
                    throw new FilterExpressionParserException($"Invalid Relationship Type UID Provided ({token.Field}).");
                }

                if (!token.IsNullValue && !Guid.TryParse(token.ValueAsString, out assetUid))
                {
                    throw new FilterExpressionParserException($"Invalid Asset UID Provided ({token.ValueAsString}).");
                }

                IntersectUids.Add(intersectUid);

                if (!token.IsNullValue)
                {
                    AssetUids.Add(assetUid);
                }
            }

            List<IntersectType> intersectTypes;
            List<Asset> filterAssets;
            List<AssetType> filterAssetTypes;

            (intersectTypes, filterAssets, filterAssetTypes) = dataProvider.GetDataForRelationshipsParsing(IntersectUids, AssetUids);

            foreach (Guid itUid in IntersectUids)
            {
                if (!intersectTypes.Any(x => x.uid == itUid))
                {
                    throw new FilterExpressionParserException($"Relationship Type with UID '{itUid.ToString()}' does not exist.");
                }
            }

            foreach (Guid assetUid in AssetUids)
            {
                if (!filterAssets.Any(x => x.uid == assetUid) && !filterAssetTypes.Any(x => x.uid == assetUid))
                {
                    throw new FilterExpressionParserException($"Asset with UID '{assetUid.ToString()}' does not exist.");
                }
            }

            //Load data to tokens
            foreach (RelationshipFieldToken token in relationshipTokens)
            {
                Guid intersectUid = Guid.Empty;
                Guid assetUid = Guid.Empty;

                Guid.TryParse(token.Field, out intersectUid);
                Guid.TryParse(token.ValueAsString, out assetUid);

                token.LoadRelationshipData(
                    intersectTypes.FirstOrDefault(x => x.uid == intersectUid),
                    filterAssets.FirstOrDefault(x => x.uid == assetUid)?.AssetType ?? filterAssetTypes.FirstOrDefault(x => x.uid == assetUid));
            }
        }
    }

    public class FilterFilterExpressionParserSettings
    {
        public FilterFilterExpressionParserSettings()
        {
            ParseType = FilterExpressionParseType.CustomFields;
            RegisterTokensAsFields = false;
            IncludeParent = false;
            UseUserDefaultFields = false;
            DefaultFilters = null;
            FieldTypes = Array.Empty<FieldType>();
            FieldColumns = Array.Empty<string>();
            FilteredFieldIDs = Array.Empty<int>();
        }

        public FilterExpressionParseType ParseType { get; set; }

        public bool RegisterTokensAsFields { get; set; }

        public bool IncludeParent { get; set; }

        public bool UseUserDefaultFields { get; set; }

        public IReadOnlyList<DefaultFilter> DefaultFilters { get; set; }

        public IReadOnlyList<FieldType> FieldTypes { get; set; }

        public IReadOnlyList<string> FieldColumns { get; set; }

        public IReadOnlyList<int> FilteredFieldIDs { get; set; }
    }

    public class FilterExpressionParser2 : IFilterExpressionParser
    {
        private readonly IFilterDataProvider dataProvider;
        private readonly List<string> disallowedFieldTypes = new List<string> { "ComplexRelationLookup", "", "OwnershipLookup", "RefListRelationship" };

        private IReadOnlyList<DefaultFilter> GetAllowedDefaultFields(FilterFilterExpressionParserSettings settings)
        {
            if (settings.DefaultFilters != null)
            {
                return settings.DefaultFilters;
            }

            List<DefaultFilter> result = new List<DefaultFilter>
            {
                new DefaultFilter("Code", "A.Code", SqlFieldType.Text),
                new DefaultFilter("Color", "JSON_VALUE((select top 1 * from dbo.GetAssetColorJsonByColor(A.Color)), '$.Name')", SqlFieldType.Text),
                new DefaultFilter("[Path]", "KP.KeyPath", SqlFieldType.Text),
                new DefaultFilter("[Level]", "LVL.Level", SqlFieldType.Number),
                new DefaultFilter("uid", "A.Uid", SqlFieldType.Text)
            };

            if (settings.IncludeParent)
            {
                result.Add(new DefaultFilter("ParentDisplayName", "Parent.DisplayValue", SqlFieldType.Text));
                result.Add(new DefaultFilter("ParentUid", "Parent.Uid", SqlFieldType.Text));
            }

            result.Add(new DefaultFilter("CreatedOn", "A.CreatedOn", SqlFieldType.DateTime));
            result.Add(new DefaultFilter("UpdatedOn", "A.UpdatedOn", SqlFieldType.DateTime));

            if (settings.UseUserDefaultFields)
            {
                result.Clear();
                result.Add(new DefaultFilter("FirstName", "gr.FirstName", SqlFieldType.Text));
                result.Add(new DefaultFilter("LastName", "gr.LastName", SqlFieldType.Text));
                result.Add(new DefaultFilter("Email", "gr.Email", SqlFieldType.Text));
                result.Add(new DefaultFilter("IsAdministrator", "gr.IsAdministrator", SqlFieldType.Boolean));
                result.Add(new DefaultFilter("LastLoggedInOn", "gr.LastLoggedInOn", SqlFieldType.DateTime));
                result.Add(new DefaultFilter("CreatedOn", "gr.CreatedOn", SqlFieldType.DateTime));
                result.Add(new DefaultFilter("uid", "gr.uid", SqlFieldType.Text));
                result.Add(new DefaultFilter("State", @"(CASE gr.state 
                    WHEN 1 THEN 'Active'
                    WHEN 2 THEN 'InActive'
                    WHEN 3 THEN 'Deleted' END)", SqlFieldType.Text));
            }

            switch (settings.ParseType)
            {
                case FilterExpressionParseType.CommunityResposibilityResource:
                    result.Clear();
                    result.Add(new DefaultFilter("FirstName", "gr.FirstName + ' ' + gr.LastName", SqlFieldType.Text));
                    result.Add(new DefaultFilter("OwnedItemCount", "OC.OwnedItemCount", SqlFieldType.Number));
                    result.Add(new DefaultFilter("State", @"(CASE gr.state 
                    WHEN 1 THEN 'Active'
                    WHEN 2 THEN 'InActive'
                    WHEN 3 THEN 'Deleted' END)", SqlFieldType.Text));

                    break;

                case FilterExpressionParseType.RuleResults:
                    result.Clear();
                    result.Add(new DefaultFilter("EvaluatedAssetClass", "E.Class", SqlFieldType.AssetTypeClass));
                    result.Add(new DefaultFilter("EvaluatedAssetTypePath", "P.Path", SqlFieldType.Text));
                    result.Add(new DefaultFilter("EvaluatedAssetPath", "E.Segments", SqlFieldType.Xml));
                    result.Add(new DefaultFilter("EvaluatedAssetDisplayPath", "E.Segments", SqlFieldType.Xml));
                    result.Add(new DefaultFilter("EffectiveDate", "R.EffectiveDate", SqlFieldType.Date));
                    result.Add(new DefaultFilter("RunDate", "R.RunDate", SqlFieldType.DateTime));
                    result.Add(new DefaultFilter("PassCount", "R.PassCount", SqlFieldType.Number));
                    result.Add(new DefaultFilter("FailCount", "R.FailCount", SqlFieldType.Number));
                    result.Add(new DefaultFilter("TotalCount", "R.TotalCount", SqlFieldType.Number));
                    result.Add(new DefaultFilter("PassFraction", "R.PassFraction", SqlFieldType.Decimal));
                    result.Add(new DefaultFilter("Outdated", "coalesce(E.IsDuplicate, R.IsDuplicate)", SqlFieldType.Boolean));

                    break;

                case FilterExpressionParseType.RelationshipCustomFields:
                    result.Add(new DefaultFilter("State", @"(CASE I.State 
                    WHEN 1 THEN 'Active'
                    WHEN 2 THEN 'InActive'
                    WHEN 3 THEN 'Deleted' END)", SqlFieldType.Text));

                    result.Add(new DefaultFilter("Object.[Path]", "ANDP_Object.DisplayPath", SqlFieldType.Text));
                    result.Add(new DefaultFilter("Subject.[Path]", "ANDP_Subject.DisplayPath", SqlFieldType.Text));
                    break;
            }

            return result;
        }

        public FilterExpressionParser2(IFilterDataProvider fdp)
        {
            dataProvider = fdp;
        }

        public string Parse(string filterString, out Dictionary<string, object> sqlParams, out IList<int> fieldIds, FilterFilterExpressionParserSettings settings = default)
        {
            if (settings == null)
            {
                settings = new FilterFilterExpressionParserSettings();
            }

            try
            {
                fieldIds = new List<int>();
                sqlParams = new Dictionary<string, object>();

                if (string.IsNullOrEmpty(filterString))
                {
                    return "";
                }

                filterString = filterString.Trim();
                StringBuilder sb = new StringBuilder();
                IList<IFilterToken> filterTokens = Tokenize(filterString, settings, fieldIds);

                LoadRelationshipDataForTokens(filterTokens);

                foreach (IFilterToken token in filterTokens)
                {
                    sb.Append($"{token.GetSqlExpression(sqlParams)}");
                }

                return sb.ToString();
            }
            catch (IndexOutOfRangeException)
            {
                throw new FilterExpressionParserException("Invalid filter expression: ", new Exception("One or more filter expressions has missing operator or value."));
            }
            catch (FilterExpressionParserException ex)
            {
                throw new FilterExpressionParserException("Invalid filter expression: " + ex.Message);
            }
            catch (Exception ex)
            {
                throw new FilterExpressionParserException("Invalid filter expression: " + ex.Message);
            }
        }

        private IList<IFilterToken> Tokenize(string filterString, FilterFilterExpressionParserSettings settings, IList<int> fieldIds)
        {
            List<IFilterToken> ret = new List<IFilterToken>();
            Regex regex = new Regex(@"\'(.+?)\'");
            MatchCollection matchGroups = regex.Matches(filterString);

            List<Tuple<string, string>> valuesMap = new List<Tuple<string, string>>();
            for (int j = 0; j < matchGroups.Count; j++)
            {
                string key = "#valueToken" + Guid.NewGuid();
                string matchValue = matchGroups[j].Value;
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
                    string value = valuesMap.FirstOrDefault(x => x.Item1.ToLower() == tokens[j].ToLowerInvariant()).Item2;
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
                    ret.Add(new OperatorToken(dataProvider, null, "(", null));
                    i++;
                    continue;
                }

                if (tokens[i] == ")")
                {
                    ret.Add(new OperatorToken(dataProvider, null, ")", null));
                    i++;
                    continue;
                }

                if (!expectingCondition)
                {
                    paramCount++;
                    switch (settings.ParseType)
                    {
                        case FilterExpressionParseType.Relationships:
                            ret.Add(new RelationshipFieldToken(dataProvider, tokens[i], tokens[i + 1], tokens[i + 2], paramCount));
                            break;
                        default:
                            ret.Add(GetFilterForTokens(settings, fieldIds, dataProvider, tokens[i], tokens[i + 1], tokens[i + 2], paramCount));
                            break;
                    }

                    expectingCondition = true;
                    i += 3;
                    continue;
                }

                if (expectingCondition)
                {
                    ret.Add(new OperatorToken(dataProvider, null, tokens[i], null));
                    expectingCondition = false;
                    i++;
                    continue;
                }
            }

            return ret;
        }

        private IFilterToken GetFilterForTokens(FilterFilterExpressionParserSettings settings, IList<int> filedIds, IFilterDataProvider fdp, string field, string op, object value,
            int? paramIdx = null)
        {
            string fieldName = field.ToLower(System.Globalization.CultureInfo.InvariantCulture);
            FieldType fieldType = settings.FieldTypes.FirstOrDefault(x => x.Name.ToLower() == fieldName);
            IReadOnlyList<DefaultFilter> allowedDefaultFields = GetAllowedDefaultFields(settings);

            if (fieldType != null && disallowedFieldTypes.Contains(fieldType.Type))
            {
                throw new FilterExpressionParserException("Field with name '" + fieldName + "' is not supported (" + fieldType.Type + ")!");
            }

            if (fieldType == null)
            {
                if (allowedDefaultFields.Any(x => x.ApiName.ToLowerInvariant() == fieldName.ToLowerInvariant()))
                {
                    DefaultFilter val = allowedDefaultFields.FirstOrDefault(x => x.ApiName.ToLowerInvariant() == fieldName.ToLowerInvariant());
                    
                    return new DefaultFieldToken(fdp, field, op, value, val, paramIdx);
                }
                else if (settings.RegisterTokensAsFields == true)
                {
                    DefaultFilter val = new DefaultFilter(fieldName, fieldName, SqlFieldType.Text);
                    
                    return new DefaultFieldToken(fdp, field, op, value, val, paramIdx);
                }
                else if (fieldName.StartsWith("$owned"))
                {
                    return new OwnerFieldToken(fdp, field, op, value, paramIdx);
                }
                else if (fieldName.StartsWith("$related"))
                {
                    return new RelationshipFieldToken(fdp, field, op, value, paramIdx);
                }
                else
                {
                    throw new FilterExpressionParserException("Field with name '" + fieldName + "' does not exist!");
                }
            }
            else
            {
                filedIds.Add(fieldType.ID);
                if (settings.ParseType == FilterExpressionParseType.ComplexLookupField)
                {
                    if (fieldName.StartsWith("$related"))
                    {
                        return new RelationshipComplexFieldToken(fdp, field, op, value, settings.FieldTypes);
                    }

                    ComplexFieldToken token = new ComplexFieldToken(fdp, field, op, value, paramIdx);
                    token.LoadFieldType(fieldType, settings.FieldColumns);
                    
                    return token;
                }
                else
                {
                    FieldToken token = new FieldToken(fdp, field, op, value, paramIdx);
                    token.LoadFieldType(fieldType, settings.FieldColumns);
                    
                    return token;
                }
            }
        }

        private string[] GetTokens(ref string filterString)
        {
            int[] replaceIndexes = GetAllIndexesOf('\'', filterString);
            int length = filterString.Length;
            for (int i = 0; i < replaceIndexes.Length; i += 2)
            {
                string subString = filterString.Substring(replaceIndexes[i], replaceIndexes[i + 1] - replaceIndexes[i]);
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
                if (c == '(')
                {
                    bracketCount++;
                }

                if (c == ')')
                {
                    bracketCount--;
                }

                if (c == '\'')
                {
                    apostropheCount++;
                }
            }

            return bracketCount == 0 && apostropheCount % 2 == 0;
        }

        private int[] GetAllIndexesOf(char c, string s)
        {
            List<int> indx = new List<int>();
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == c)
                {
                    indx.Add(i);
                }
            }

            return indx.ToArray();
        }

        private void LoadRelationshipDataForTokens(IEnumerable<IFilterToken> tokens)
        {
            List<RelationshipFieldToken> relationshipTokens = tokens.OfType<RelationshipFieldToken>().ToList();
            if (relationshipTokens.Count == 0)
            {
                return;
            }

            List<Guid> IntersectUids = new List<Guid>();
            List<Guid> AssetUids = new List<Guid>();

            foreach (RelationshipFieldToken token in relationshipTokens)
            {
                Guid intersectUid = Guid.Empty;
                Guid assetUid = Guid.Empty;

                if (!Guid.TryParse(token.Field, out intersectUid))
                {
                    throw new FilterExpressionParserException($"Invalid Relationship Type UID Provided ({token.Field}).");
                }

                if (!token.IsNullValue && !Guid.TryParse(token.ValueAsString, out assetUid))
                {
                    throw new FilterExpressionParserException($"Invalid Asset UID Provided ({token.ValueAsString}).");
                }

                IntersectUids.Add(intersectUid);

                if (!token.IsNullValue)
                {
                    AssetUids.Add(assetUid);
                }
            }

            List<IntersectType> intersectTypes;
            List<Asset> filterAssets;
            List<AssetType> filterAssetTypes;

            (intersectTypes, filterAssets, filterAssetTypes) = dataProvider.GetDataForRelationshipsParsing(IntersectUids, AssetUids);

            foreach (Guid itUid in IntersectUids)
            {
                if (intersectTypes.All(x => x.uid != itUid))
                {
                    throw new FilterExpressionParserException($"Relationship Type with UID '{itUid.ToString()}' does not exist.");
                }
            }

            foreach (Guid assetUid in AssetUids)
            {
                if (filterAssets.All(x => x.uid != assetUid) && filterAssetTypes.All(x => x.uid != assetUid))
                {
                    throw new FilterExpressionParserException($"Asset with UID '{assetUid.ToString()}' does not exist.");
                }
            }

            //Load data to tokens
            foreach (RelationshipFieldToken token in relationshipTokens)
            {
                Guid intersectUid = Guid.Empty;
                Guid assetUid = Guid.Empty;

                Guid.TryParse(token.Field, out intersectUid);
                Guid.TryParse(token.ValueAsString, out assetUid);

                token.LoadRelationshipData(
                    intersectTypes.FirstOrDefault(x => x.uid == intersectUid),
                    filterAssets.FirstOrDefault(x => x.uid == assetUid)?.AssetType ?? filterAssetTypes.FirstOrDefault(x => x.uid == assetUid));
            }
        }
    }

    public interface IFilterExpressionParser
    {
        string Parse(
            string filterString,
            out Dictionary<string, object> sqlParams,
            out IList<int> fieldIds,
            FilterFilterExpressionParserSettings settings = default
        );
    }
}
