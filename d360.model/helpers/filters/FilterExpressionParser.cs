using d360.core.entities;
using d360.model.helpers.filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace d360.model.helpers
{
    public class FilterExpressionParser
    {
        private readonly IFilterDataProvider dataProvider;
        private List<FieldType> fieldTypes = new List<FieldType>();
        private List<string> fieldColumns = new List<string>();
        private List<int> filteredFieldIDs = new List<int>();
        private FilterExpressionParseType parseType;
        private List<DefaultFilter> allowedDefaultFields = new List<DefaultFilter>();
        private List<string> disallowedFieldTypes = new List<string>() { "ComplexRelationLookup", "", "OwnershipLookup", "RefListRelationship" };

        private bool registerTokensAsFields = false;

        public FilterExpressionParser(
            IFilterDataProvider fdp,
            FilterExpressionParseType type = FilterExpressionParseType.CustomFields,
            bool includeParent = false,
            bool useUserDefaultFields = false,
            bool registerTokensAsFields = false
            )
        {
            this.dataProvider = fdp;
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

        public string Parse(string filterString, out Dictionary<string, object> sqlParams, out List<int> fieldIds)
        {
            try
            {
                fieldIds = this.filteredFieldIDs;
                
                sqlParams = new Dictionary<string, object>();
                if (string.IsNullOrEmpty(filterString))
                {
                    return "";
                }

                filterString = filterString.Trim();

                StringBuilder sb = new StringBuilder();

                List<IFilterToken> filterTokens = Tokenize(filterString);

                this.LoadRelationshipDataForTokens(filterTokens);

                foreach (var token in filterTokens)
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
            var ret = new List<IFilterToken>();
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
                    ret.Add(new OperatorToken(this.dataProvider, null, "(", null));
                    i++;
                    continue;
                }
                if (tokens[i] == ")")
                {
                    ret.Add(new OperatorToken(this.dataProvider, null, ")", null));
                    i++;
                    continue;
                }

                if (!expectingCondition)
                {
                    paramCount++;
                    if (parseType == FilterExpressionParseType.Relationships)
                    {
                        ret.Add(new RelationshipFieldToken(this.dataProvider, tokens[i], tokens[i + 1], tokens[i + 2], paramCount));
                    }
                    else
                    {
                        ret.Add(this.GetFilterForTokens(this.dataProvider, tokens[i], tokens[i + 1], tokens[i + 2], paramCount));
                    }
                    expectingCondition = true;
                    i += 3;
                    continue;
                }

                if (expectingCondition)
                {
                    ret.Add(new OperatorToken(this.dataProvider, null, tokens[i], null));
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
            var fieldName = field.ToLower(System.Globalization.CultureInfo.InvariantCulture);
            var fieldType = this.fieldTypes.FirstOrDefault(x => x.Name.ToLower() == fieldName);

            if (fieldType != null && disallowedFieldTypes.Contains(fieldType.Type))
            {
                throw new FilterExpressionParserException("Field with name '" + fieldName + "' is not supported (" + fieldType.Type + ")!");
            }

            if (fieldType == null)
            {
                if (allowedDefaultFields.Any(x => x.ApiName.ToLowerInvariant() == fieldName.ToLowerInvariant()))
                {
                    var val = allowedDefaultFields.FirstOrDefault(x => x.ApiName.ToLowerInvariant() == fieldName.ToLowerInvariant());
                    return new DefaultFieldToken(fdp, field, op, value, val, paramIdx);
                }
                else if (this.registerTokensAsFields == true)
                {
                    var val = new DefaultFilter(fieldName, fieldName, SqlFieldType.Text);
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
                this.filteredFieldIDs.Add(fieldType.ID);
                if (parseType == FilterExpressionParseType.ComplexLookupField)
                {
                    if (fieldName.StartsWith("$related"))
                    {
                        return new RelationshipComplexFieldToken(fdp, field, op, value, this.fieldTypes);
                    }
                    var token = new ComplexFieldToken(fdp, field, op, value, paramIdx);
                    token.LoadFieldType(fieldType, fieldColumns);
                    return token;
                }
                else
                {
                    var token = new FieldToken(fdp, field, op, value, paramIdx);
                    token.LoadFieldType(fieldType, fieldColumns);
                    return token;
                }
            }
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

        private void LoadRelationshipDataForTokens(List<IFilterToken> tokens)
        {
            var relationshipTokens = tokens.Where(x => x is RelationshipFieldToken).ToList();
            if (relationshipTokens.Count == 0)
            {
                return;
            }

            List<Guid> IntersectUids = new List<Guid>();
            List<Guid> AssetUids = new List<Guid>();

            foreach (RelationshipFieldToken token in relationshipTokens)
            {
                var intersectUid = Guid.Empty;
                var assetUid = Guid.Empty;

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

            (intersectTypes, filterAssets, filterAssetTypes) = this.dataProvider.GetDataForRelationshipsParsing(IntersectUids, AssetUids);

            foreach (var itUid in IntersectUids)
            {
                if (!intersectTypes.Any(x => x.uid == itUid))
                {
                    throw new FilterExpressionParserException($"Relationship Type with UID '{itUid.ToString()}' does not exist.");
                }
            }

            foreach (var assetUid in AssetUids)
            {
                if (!filterAssets.Any(x => x.uid == assetUid) && !filterAssetTypes.Any(x => x.uid == assetUid))
                {
                    throw new FilterExpressionParserException($"Asset with UID '{assetUid.ToString()}' does not exist.");
                }
            }

            //Load data to tokens
            foreach (RelationshipFieldToken token in relationshipTokens)
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
}
