using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.model.helpers.filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace d360.model.helpers
{
    public class FilterExpressionParser
    {
        private readonly IFilterDataProvider dataProvider;
        private List<FieldType> fieldTypes = new List<FieldType>();
        private List<string> fieldColumns = new List<string>();
        private readonly List<int> filteredFieldIDs = new List<int>();
        public List<string> filteredCustomFields = new List<string>();
        private readonly FilterExpressionParseType parseType;
        private readonly List<DefaultFilter> allowedDefaultFields = new List<DefaultFilter>();
        private readonly List<string> disallowedFieldTypes = new List<string> { "ComplexRelationLookup", "", "OwnershipLookup", "RefListRelationship" };

        private readonly bool registerTokensAsFields;
        private readonly bool allowTempTableFiltering;

		private bool hasSingleParentFilter;
		List<IFilterToken> filterTokens = new List<IFilterToken>();
		DynamicQueryJoins dynamicQueryJoins;

		List<Guid> AssetTypeLevels;
		List<AssetTypeKeyFieldMap> AssetTypeKeyFieldMaps;

		public FilterExpressionParser(
            IFilterDataProvider fdp,
            FilterExpressionParseType type = FilterExpressionParseType.CustomFields,
            bool includeParent = false,
            bool useUserDefaultFields = false,
            bool registerTokensAsFields = false,
			bool allowTempTableFiltering = false
        )
        {
            dataProvider = fdp;
            parseType = type;
			this.allowTempTableFiltering = allowTempTableFiltering;
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
            allowedDefaultFields.Add(new DefaultFilter("CreatedBy", "A.CreatedBy", SqlFieldType.Number));
            allowedDefaultFields.Add(new DefaultFilter("LastModifiedBy", "A.UpdatedBy", SqlFieldType.Number));

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
                allowedDefaultFields.Add(new DefaultFilter("createdOn", "CAST(CreatedOn as DATE)", SqlFieldType.Date));
                allowedDefaultFields.Add(new DefaultFilter("updatedOn", "CAST(UpdatedOn as DATE)", SqlFieldType.Date));
                allowedDefaultFields.Add(new DefaultFilter("createdBy", "CreatedBy", SqlFieldType.Text));
                allowedDefaultFields.Add(new DefaultFilter("updatedBy", "UpdatedBy", SqlFieldType.Text));

				//I understand it is a copy of the item above.
				//The reason to add a new one is to avoid side effects that may be caused by modifying the old thing.
				//Also, I don't want to alter the UI component to send the `updatedBy` instead of the `lastModifiedBy` filter
				//because there is no reason to send different filter titles for the same filter item.
				allowedDefaultFields.Add(new DefaultFilter("lastModifiedBy", "UpdatedBy", SqlFieldType.Text));
            }

            if (parseType == FilterExpressionParseType.Tags)
            {
                allowedDefaultFields.Clear();
                allowedDefaultFields.Add(new DefaultFilter("value", "t.Value", SqlFieldType.Text));
                allowedDefaultFields.Add(new DefaultFilter("useCount", "Tags.count", SqlFieldType.Number));
                allowedDefaultFields.Add(new DefaultFilter("createdOn", "cast(t.CreatedOn as date)", SqlFieldType.Date));
                allowedDefaultFields.Add(new DefaultFilter("createdBy", "grc.FirstName + ' ' +grc.LastName", SqlFieldType.Text));
            }

            if (parseType == FilterExpressionParseType.TagDetails)
            {
                allowedDefaultFields.Clear();
                allowedDefaultFields.Add(new DefaultFilter("displayPath", "DisplayPath", SqlFieldType.Text));
                allowedDefaultFields.Add(new DefaultFilter("assetType", "AST.Name", SqlFieldType.Text));
                allowedDefaultFields.Add(new DefaultFilter("createdOn", "cast(AT.CreatedOn as date)", SqlFieldType.Date));
                allowedDefaultFields.Add(new DefaultFilter("addedByUid", "grc.uid", SqlFieldType.Guid));
            }
        }

        public void OverrideAllowedDefaultFields(List<DefaultFilter> defaultFilters)
        {
            allowedDefaultFields.Clear();
            allowedDefaultFields.AddRange(defaultFilters);
        }

        public void LoadFieldTypes(List<FieldType> fields, List<string> columns, DynamicQueryJoins queryJoins = null)
        {
            fieldTypes = fields;
            fieldColumns = columns;
			dynamicQueryJoins = queryJoins;
		}

        public string Parse(string filterString, out Dictionary<string, object> sqlParams, out List<int> fieldIds)
        {
            try
            {
				fieldIds = filteredFieldIDs;
				filteredCustomFields.Clear();

				sqlParams = new Dictionary<string, object>();
                if (string.IsNullOrEmpty(filterString))
                {
                    return "";
                }

				if (fieldTypes.Any(x => x.IsPathSegment))
				{
					List<Guid> assetTypeUids = new List<Guid>();
					foreach (var ft in fieldTypes.Where(x => x.IsPathSegment))
					{
						var definition = JsonConvert.DeserializeObject<JObject>(ft.Definition);
						assetTypeUids.Add(Guid.Parse(definition.GetValue("AssetTypeUid").ToString()));
					}

					(AssetTypeLevels, AssetTypeKeyFieldMaps) = dataProvider.GetPathSegmentsMappingInfo(fieldTypes.First().AssetTypeID.Value, assetTypeUids);
				}

				filterString = filterString.Trim();
				hasSingleParentFilter = filterString.ToLowerInvariant().Split(new [] { "parentuid" }, StringSplitOptions.None).Length == 2;

				StringBuilder sb = new StringBuilder();

				filterTokens = Tokenize(filterString);

                LoadRelationshipDataForTokens(filterTokens);

				foreach (IFilterToken token in filterTokens)
                {
					sb.Append($"{token.GetSqlExpression(sqlParams)}");
                }

				var query = $"({sb.ToString()})";

				return query;
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
					filteredCustomFields.Add(fieldName);

					if (fieldName.ToLowerInvariant() == "parentuid" && hasSingleParentFilter && (op == "eq" || op == "ne"))
					{
						//for only single parentuid filter use token which applies filter with temp tables
						return new ChildFilterToken(fdp, field, op, value, val, paramIdx);
					}

					return new DefaultFieldToken(fdp, field, op, value, val, paramIdx);
                }
                else if (registerTokensAsFields == true)
                {
                    DefaultFilter val = new DefaultFilter(fieldName, fieldName, SqlFieldType.Text);
					filteredCustomFields.Add(fieldName);

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
				List<string> filterTypesWithTempTables = new List<string>
				{
					DataType.Text.ToString(),
					DataType.Number.ToString(),
					DataType.Lookup.ToString(),
					DataType.Boolean.ToString(),
					DataType.Counter.ToString(),
					DataType.Date.ToString(),
					DataType.DateTime.ToString(),
					DataType.Decimal.ToString(),
					DataType.Html.ToString(),
					DataType.Number.ToString(),
					DataType.Tag.ToString(),
					DataType.Link.ToString(),
					DataType.Score.ToString()
				};

                if (parseType == FilterExpressionParseType.ComplexLookupField)
                {
					filteredFieldIDs.Add(fieldType.ID);

					if (fieldName.StartsWith("$related"))
                    {
                        return new RelationshipComplexFieldToken(fdp, field, op, value, fieldTypes);
                    }

                    ComplexFieldToken token = new ComplexFieldToken(fdp, field, op, value, paramIdx);
                    token.LoadFieldType(fieldType, fieldColumns);
                    
                    return token;
                }
				else if (allowTempTableFiltering && fieldType.IsPathSegment)
				{
					filteredFieldIDs.Add(fieldType.ID);

					TempTablePathSegmentToken token = new TempTablePathSegmentToken(fdp, field, op, value, AssetTypeKeyFieldMaps, paramIdx);
					token.LoadFieldType(fieldType, fieldColumns);

					return token;
				}
				else if (allowTempTableFiltering && filterTypesWithTempTables.Contains(fieldType.Type))
				{
					filteredFieldIDs.Add(fieldType.ID);

					TempTableFieldToken token = new TempTableFieldToken(fdp, field, op, value, paramIdx);
					token.LoadFieldType(fieldType, fieldColumns, dynamicQueryJoins);

					return token;
				}
				else
                {
					filteredFieldIDs.Add(fieldType.ID);

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

		public AdvancedFilterTempTableFilters GetAdvancedFilterTempTableFilters()
		{
			var data = new AdvancedFilterTempTableFilters();

			if (filterTokens.Any(x => x is ISegmentPathFilterToken))
			{
				List<Guid> assetTypeUids = new List<Guid>();
				foreach (var ft in fieldTypes.Where(x => x.IsPathSegment))
				{
					var definition = JsonConvert.DeserializeObject<JObject>(ft.Definition);
					assetTypeUids.Add(Guid.Parse(definition.GetValue("AssetTypeUid").ToString()));
				}

				if (AssetTypeLevels.Count > 0)
				{
					StringBuilder sb = new StringBuilder();

					sb.AppendLine(@"
								drop table if exists #parent_relationships
								select IT.ID
								into #parent_relationships
								from [IntersectType] IT 
								inner join [Predicate] P on P.ID  = IT.PredicateID AND p.Type in (3,4)");

					List<string> temp_table_columns = new List<string>();
					List<string> targetJoins = new List<string>();

					temp_table_columns.Add("AssetId int");

					for (int i = 0; i < AssetTypeLevels.Count; i++)
					{
						temp_table_columns.Add($"[lvl_{AssetTypeLevels[i]}] int");
						targetJoins.Add($"ATarget{i+1}.ID");
					}

					sb.AppendLine($@"
								drop table if exists #assets_hierarchy
								create table #assets_hierarchy({string.Join(",", temp_table_columns)})");

					sb.AppendLine(@$"insert into #assets_hierarchy
											select a.id,{string.Join(",", targetJoins)}
											from asset a
											left join[Intersect] I1 on I1.ObjectAssetID = A.ID and I1.IntersectTypeID in (select id from #parent_relationships)");
					for (int i = 2; i <= AssetTypeLevels.Count; i++)
					{
						sb.AppendLine($"left join[Intersect] I{i} on I{i}.ObjectAssetID = I{i - 1}.SubjectAssetID and I{i}.IntersectTypeID in (select id from #parent_relationships)");
					}
					for (int i = 1; i <= AssetTypeLevels.Count; i++)
					{
						sb.AppendLine($"left join Asset ATarget{i} on ATarget{i}.ID = I{i}.SubjectAssetID");
					}
					sb.Append("where a.AssetTypeID = @assettypeid");

					data.Add(new AdvancedFilterTempTableInfo { TempTableQuery = sb.ToString() });
				}
			}

			foreach(var token in filterTokens)
			{
				if (token is ITempTableFilter)
				{
					data.Add((token as ITempTableFilter).GetTempTableFilterData());
				}
			}

			return data;
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
