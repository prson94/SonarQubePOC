using d360.core.entities;
using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.helpers
{
    public class FilterToken
    {
        private ICompanyContext CompanyContext;


        private int parameterIdx { get; set; }
        private string field { get; set; }
        public string @operator { get; set; }
        private object value { get; set; }
        private bool isNullValue { get; set; }
        private FieldType fieldType { get; set; }
        private string fieldColumn { get; set; }
        private bool isLookupField { get; set; }
        private StringBuilder stringBuilder = new StringBuilder();
        private Dictionary<string, object> sqlParamsRef;
        private bool convertToNVarChar = false;

        private AssetType assetType { get; set; }
        private IntersectType intersectType { get; set; }

        public bool IsOnlyOperator
        {
            get
            {
                return field == null && value == null;
            }
        }

        public bool IsOwnerFilter
        {
            get
            {
                return field == "$ownedby";
            }
        }

        public bool IsRlationshipFilter
        {
            get
            {
                return field.StartsWith("$related");
            }
        }

        public string Field
        {
            get
            {
                return field;
            }
        }
        public string ValueAsString
        {
            get
            {
                return value.ToString();
            }
        }

        public FilterToken(ICompanyContext ctx, string field, string op, object value, int? paramIdx = null)
        {
            CompanyContext = ctx;
            parameterIdx = paramIdx ?? -1;
            this.field = field;
            @operator = op;
            this.value = value;

            if (this.value != null && this.value.ToString().ToLower(CultureInfo.InvariantCulture) == "null")
            {
                this.isNullValue = true;
            }
        }

        public string GetSQLForField(ref Dictionary<string, object> sqlParams)
        {
            if (field == null)
            {
                throw new MethodAccessException("Method can be used only when Field Type is loaded. Use LoadFieldType() method before.");
            }
            sqlParamsRef = sqlParams;
            stringBuilder.Clear();
            if (!this.isNullValue)
            {
                ValidateTokenForType();
                UpdateTokenValueForType();
            }
            else
            {
                UpdateTokenForNullValue();
            }
            return stringBuilder.ToString();
        }

        public string GetSQLForOperator()
        {
            if (!IsOnlyOperator)
            {
                throw new MethodAccessException("Method can be used only for non field tokens");
            }
            stringBuilder.Clear();
            if (@operator != "(" && @operator != ")")
            {
                stringBuilder.Append(GetLogicalOperator(@operator));
            }
            else
            {
                stringBuilder.Append(@operator);
            }
            return stringBuilder.ToString();
        }

        public string GetSQLForDefaultField(ref Dictionary<string, object> sqlParams, DefaultFilter filter)
        {
            this.sqlParamsRef = sqlParams;

            if (!IsValidOperatorForFieldType(filter))
            {
                throw new Exception($"Operator '{@operator}' is not valid for '{filter.SqlFieldType.ToString().ToLower()}' on field {field}");
            }

            CheckFieldValue(filter);

            value = value.ToString().Trim('\'');
            if (this.@operator == "ct" || this.@operator == "nct")
            {
                value = $"%{wildcardValue(escapeForSQLLike(value.ToString()))}%";
            }

            stringBuilder.Clear();

            if (this.convertToNVarChar)
            {
                filter.SqlExpression = $"CONVERT(VARCHAR,{filter.SqlExpression},120)";
            }

            stringBuilder.Append(filter.SqlExpression);
            stringBuilder.Append(GetSQLOperator(@operator));
            stringBuilder.Append($"@filter_{parameterIdx}");

            sqlParamsRef.Add($"@filter_{parameterIdx}", value);
            return stringBuilder.ToString();
        }

        public string GetSQLForOwnerField(ref Dictionary<string, object> sqlParams)
        {
            this.sqlParamsRef = sqlParams;
            stringBuilder.Clear();
            var value = this.value.ToString().Trim('\'');
            sqlParamsRef.Add($"@filter_{parameterIdx}", value);

            string querySql = $@"EXISTS(
                                            SELECT 1 
                                            FROM 
                                                [dbo].[ResponsibilityDetail] rd 
                                            WHERE 
                                                rd.SecurityAssetUid = @filter_{parameterIdx}
                                                and 
                                                a.ID=rd.AssetID 
                                                and
                                                rd.isVisible = 1
                                            UNION
                                            SELECT 1 
                                            FROM 
                                                [dbo].[ResponsibilityDetail] rd 
                                            WHERE 
                                                rd.SecurityAssetUid = @filter_{parameterIdx} 
                                                and 
                                                rd.ApplyToType = 1 
                                                and 
                                                rd.AssetID = 0 
                                                and 
                                                rd.AssetTypeId=a.AssetTypeId
                                                and
                                                rd.isVisible = 1
                                            )";

            if (this.@operator == "ne")
            {
                querySql = " NOT " + querySql;
            }

            return querySql;
        }

        public string GetSQLForRelationField(ref Dictionary<string, object> sqlParams)
        {
            this.sqlParamsRef = sqlParams;
            stringBuilder.Clear();
            var origQuery = $"{this.field} {this.@operator} {this.value.ToString().Trim('\'')}";

            var filterExpressionParser = new FilterExpressionParser(CompanyContext, FilterExpressionParseType.Relationships);
            Dictionary<string, object> _sqlParams = new Dictionary<string, object>();
            List<int> filteredFields = new List<int>();
            var query = filterExpressionParser.Parse(origQuery.Replace("$related:", ""), out _sqlParams, out filteredFields);

            foreach (var item in _sqlParams)
            {
                string updatedKey = item.Key + "_" + parameterIdx;
                query = query.Replace(item.Key, updatedKey);
                this.sqlParamsRef.Add(updatedKey, item.Value);
            }
            return query;
        }

        public string GetSQLForRelationship(ref Dictionary<string, object> sqlParams)
        {

            if (assetType == null || intersectType == null)
            {
                throw new MethodAccessException("Method can be used only when Intersect Type and Asset Type are loaded. Use LoadRelationshipData() method before.");
            }

            this.sqlParamsRef = sqlParams;
            stringBuilder.Clear();

            if (!new string[] { "eq", "ne" }.Contains(@operator))
            {
                throw new Exception($"Operator '{@operator}' is not valid when filtering relationship. Use 'eq' or 'ne'.");
            }

            var condition = @operator == "eq" ? " exists" : " not exists";

            var filterCond = GetSplitFilterCriteriaRelationship();
            var hasRefList = (intersectType.Object == "ReferenceItemType" && intersectType.ObjectID == 0) || (intersectType.Subject == "ReferenceItemType" && intersectType.SubjectID == 0);

            if (!hasRefList)
            {
                AddRelationshipFilterWithGraphTables(condition, filterCond);
            }
            else
            {
                stringBuilder.Append($@"{condition} (select AT.Uid from [IntersectType] IT
	            left join [Intersect] I1 on I1.IntersectTypeID = IT.ID and I1.Object = A.Object and I1.ObjectId = A.ObjectID
	            left join [Intersect] I2 on I2.IntersectTypeID = IT.ID and I2.Subject = A.Object and I2.SubjectID = A.ObjectID
	            inner join AssetType AT on AT.Object = ISNULL(I1.Subject,I2.Object) and AT.ObjectID = ISNULL(I1.SubjectId, I2.ObjectID)
	            where IT.Uid = @intersectFilter{this.parameterIdx} and AT.Uid = @intersectAssetFilter{this.parameterIdx})");
            }


            sqlParams.Add($"@intersectFilter{this.parameterIdx}", Guid.Parse(field));
            sqlParams.Add($"@intersectAssetFilter{this.parameterIdx}", Guid.Parse(ValueAsString));
            return stringBuilder.ToString();
        }

        private void AddRelationshipFilterWithGraphTables(string condition, SplitFilterCriteriaRelationship filterCond)
        {
            if (filterCond == SplitFilterCriteriaRelationship.Subject)
            {
                stringBuilder.Append($@"{condition}(SELECT       O.Uid as TargetAssetId
                    FROM         graph.AssetNode S, graph.AssetEdge E, graph.AssetNode O
                    WHERE        MATCH(S <- (E) - O)  AND IntersectTypeUid = @intersectFilter{this.parameterIdx}
				              AND S.Uid = A.Uid and O.Uid = @intersectAssetFilter{this.parameterIdx})");
            }
            else if (filterCond == SplitFilterCriteriaRelationship.Object)
            {
                stringBuilder.Append($@"{condition}(SELECT       O.Uid as TargetAssetId
                    FROM         graph.AssetNode S, graph.AssetEdge E, graph.AssetNode O
                    WHERE        MATCH(S - (E) -> O)  AND IntersectTypeUid = @intersectFilter{this.parameterIdx}
				              AND S.Uid = A.Uid and O.Uid = @intersectAssetFilter{this.parameterIdx})");
            }
            else
            {
                stringBuilder.Append($@"{condition}(SELECT       O.Uid as TargetAssetId
                    FROM         graph.AssetNode S, graph.AssetEdge E, graph.AssetNode O
                    WHERE        MATCH(S <- (E) - O)  AND IntersectTypeUid = @intersectFilter{this.parameterIdx}
				              AND S.Uid = A.Uid and O.Uid = @intersectAssetFilter{this.parameterIdx}
                    UNION
                    SELECT       O.Uid as TargetAssetId
                    FROM         graph.AssetNode S, graph.AssetEdge E, graph.AssetNode O
                    WHERE        MATCH(S - (E) -> O)  AND IntersectTypeUid = @intersectFilter{this.parameterIdx}
				              AND S.Uid = A.Uid and O.Uid = @intersectAssetFilter{this.parameterIdx})");
            }
        }

        public void LoadFieldType(FieldType ft, List<string> fieldColumns)
        {
            fieldType = ft;
            fieldColumn = fieldColumns.FirstOrDefault(x => x.Contains($"F" + fieldType.ID));
        }
        public void LoadRelationshipData(IntersectType it, AssetType at)
        {
            this.intersectType = it;
            this.assetType = at;
        }
        private void UpdateTokenValueForType()
        {
            CheckFieldValue();

            if (@operator == "ct" || @operator == "nct")
            {
                bool isStartWith = value.ToString().Last() == '*';
                bool isEndWith = value.ToString().First() == '*';
                bool isBoth = (isStartWith && isEndWith) || (!isStartWith && !isEndWith);

                if (isBoth)
                {
                    value = $"%{wildcardValue(escapeForSQLLike(value.ToString()))}%";
                }
                else
                {
                    //Wildcard will be present from request
                    value = $"{wildcardValue(escapeForSQLLike(value.ToString()))}";
                }
            }

            string[] lookupFieldTypes = new string[] { "Lookup", "Relationship" };

            if (lookupFieldTypes.Select(x => x.ToLower()).Contains(fieldType.Type.ToLower()))
            {
                if (fieldType.LookupObjectID == null)
                {
                    throw new Exception("Lookup field type is missing LookupObjectID value!");
                }
                this.isLookupField = true;
                LoadLookupSql();
            }

            if (!this.isLookupField)
            {
                var fieldSql = GetColumnValueSyntax(fieldType.ID);

                if (this.convertToNVarChar)
                {
                    fieldSql = $"CONVERT(VARCHAR,{fieldSql},120)";
                }

                if (this.fieldType.Type == "Score")
                {
                    fieldSql = $"CONVERT(DECIMAL(7,2),REPLACE({fieldSql},'%',''))";
                }

                stringBuilder.Append(fieldSql);
                stringBuilder.Append(GetSQLOperator(@operator));
                stringBuilder.Append($"@filter_{parameterIdx}");
            }

            sqlParamsRef.Add($"@filter_{parameterIdx}", value);

        }

        private void UpdateTokenForNullValue()
        {
            if (!(new[] { "eq", "ne" }.Contains(@operator)))
            {
                throw new FormatException($"NULL value filter can be used only with 'eq' and 'ne' operator!");
            }
            var fieldSql = GetColumnValueSyntax(fieldType.ID);

            stringBuilder.Append(fieldSql);
            stringBuilder.Append(GetSQLNullOperator(@operator));
        }

        private void CheckFieldValue(DefaultFilter filter = null)
        {
            string ft = filter == null ? fieldType.Type : filter.SqlFieldType.ToString();
            switch (ft.ToLower())
            {
                case "number":
                    int number = 0;
                    if (!int.TryParse(value.ToString(), NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out number))
                    {
                        throw new FormatException($"Invalid numeric value for field '{field}'");
                    }
                    value = number;
                    break;
                case "decimal":
                    decimal dnumber = 0;
                    if (!decimal.TryParse(value.ToString(), out dnumber))
                    {
                        throw new FormatException($"Invalid decimal value for field '{field}'");
                    }
                    value = dnumber;
                    break;
                case "boolean":
                    bool boolean = false;
                    var stringValue = value.ToString().ToLower().Trim();
                    if (stringValue == "0") stringValue = "false";
                    if (stringValue == "1") stringValue = "true";

                    if ("true".Contains(stringValue))
                    {
                        stringValue = "true";
                    }
                    if ("false".Contains(stringValue))
                    {
                        stringValue = "false";
                    }

                    if (!bool.TryParse(stringValue, out boolean))
                    {
                        throw new FormatException($"Invalid boolean value for field '{field}'");
                    }
                    value = boolean;
                    break;
                case "date":
                case "datetime":
                    DateTime date = new DateTime();
                    if (!DateTime.TryParse(value.ToString().Trim('\''), out date))
                    {
                        if (@operator == "ct" || @operator == "nct")
                        {
                            value = value.ToString().Trim('\'').Replace("&apos;", "'");
                            this.convertToNVarChar = true;
                        }
                        else
                        {
                            throw new FormatException($"Invalid date value for field '{field}'");
                        }
                    }
                    else
                    {
                        value = date;
                        if (@operator == "ct" || @operator == "nct")
                        {
                            this.convertToNVarChar = true;

                            if (date == date.Date)
                            {
                                value = date.ToString("yyyy-MM-dd");
                            }
                        }
                    }

                    break;
                default:
                    value = value.ToString().Trim('\'').Replace("&apos;", "'");
                    break;
            }
        }

        private void LoadLookupSql()
        {
            if (fieldType.Type == "Lookup")
            {
                if (@operator == "ct")
                {
                    stringBuilder.Append($"F{fieldType.ID}.FormattedValue like @filter_{parameterIdx}");
                }
                else
                {

                    int lookupValue = CompanyContext.GetFieldLookupValue(fieldType.LookupObjectType, fieldType.LookupObjectID.Value, fieldType.ID, value.ToString());
                    if (lookupValue <= 0)
                        throw new Exception($"Invalid lookup value '{value}' for field '{field}'");

                    value = lookupValue.ToString();

                    string condition = "in";
                    if (@operator == "ne")
                    {
                        condition = "not in";
                    }
                    var basicSqlExpression = string.Empty;

                    if (!string.IsNullOrEmpty(fieldType.DefaultValue))
                    {
                        basicSqlExpression = $"@filter_{parameterIdx} {condition} (select * from string_split(coalesce(F{fieldType.ID}.Value,@defLookupValue{parameterIdx}),','))";
                        sqlParamsRef.Add($"@defLookupValue{parameterIdx}", fieldType.DefaultValue);
                    }
                    else
                    {
                        basicSqlExpression = $"@filter_{parameterIdx} {condition} (select * from string_split(F{fieldType.ID}.Value,','))";
                    }

                    if (fieldType.AllowAllValue)
                    {
                        basicSqlExpression = $"(F{fieldType.ID}.Value = '0' or {basicSqlExpression})";
                    }
                    stringBuilder.Append(basicSqlExpression);

                }
            }

            if (fieldType.Type == "Relationship")
            {
                string condition = "exists";
                if (@operator == "ne")
                {
                    condition = "not exists";
                }

                var whereStatement = $@"{condition}
                                    (select id from intersectdetail where intersecttypeid = {fieldType.LookupObjectID} and subjectuid = a.uid and subjecttypeid = T.ObjectId and subjecttype = T.Object and objectname {(@operator == "ct" ? "like" : "=")} @filter_{parameterIdx}
                                    union select id from IntersectDetail where intersecttypeid = {fieldType.LookupObjectID} and objectuid = a.uid and objecttypeid = T.ObjectId and objecttype = T.Object and subjectname {(@operator == "ct" ? "like" : "=")} @filter_{parameterIdx})";

                stringBuilder.Append(whereStatement);
            }
        }

        private void ValidateTokenForType()
        {
            bool hasApostrophe = value.ToString().First() == '\'' && value.ToString().Last() == '\'';
            if (!hasApostrophe && !(fieldType.Type == "Number" || fieldType.Type == "Decimal" || fieldType.Type == "Boolean" || fieldType.Type == "Score"))
            {
                throw new Exception("Text values should be placed within quotations.");
            }

            if (!IsValidOperatorForFieldType())
            {
                throw new Exception($"Operator '{@operator}' is not valid for '{fieldType.Type}' on field {field}");
            }
        }

        private bool IsValidOperatorForFieldType(DefaultFilter defaultFilter = null)
        {
            var operand = @operator.ToLower();
            string fType = defaultFilter == null ? fieldType.Type.ToLower() : defaultFilter.SqlFieldType.ToString().ToLower();

            switch (fType)
            {
                case "boolean":
                case "lookup":
                case "relationship":
                    return new string[] { "eq", "ne", "ct" }.Contains(operand);
                case "number":
                case "decimal":
                case "score":
                    return !(new string[] { "ct", "nct" }.Contains(operand));
                case "date":
                case "datetime":
                    return true;
                default:
                    return new string[] { "eq", "ne", "ct", "nct" }.Contains(operand);
            }
        }

        private string GetColumnValueSyntax(int fieldTypeId)
        {
            if (fieldType.Type == "Path")
            {
                return $"Node.DisplayPath";
            }
            else
            {
                if (fieldColumn == null || fieldColumn.LastIndexOf(" as ") <= 0)
                {
                    return $"F{fieldTypeId}.FormattedValue";
                }
                return fieldColumn.Substring(0, fieldColumn.LastIndexOf(" as "));
            }
        }

        private string GetSQLOperator(string value)
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
                default: throw new Exception($"Invalid comparison operator '{value}'");
            }
        }

        private string GetSQLNullOperator(string value)
        {
            switch (value)
            {
                case "eq": return " is null";
                case "ne": return " is not null";
                default: throw new Exception($"Invalid comparison operator '{value}'");
            }
        }

        private string GetLogicalOperator(string value)
        {
            switch (value)
            {
                case "and": return " and ";
                case "or": return " or ";
                default: throw new Exception($"Invalid logical operator '{value}'");
            }
        }

        private SplitFilterCriteriaRelationship GetSplitFilterCriteriaRelationship()
        {
            if (intersectType.Object == assetType.Object && intersectType.ObjectID == assetType.ObjectID
               && intersectType.Subject == assetType.Object && intersectType.SubjectID == assetType.ObjectID)
            {
                return SplitFilterCriteriaRelationship.Both;
            }
            if (intersectType.Object == assetType.Object && intersectType.ObjectID == assetType.ObjectID)
            {
                return SplitFilterCriteriaRelationship.Object;
            }
            else
                return SplitFilterCriteriaRelationship.Subject;

        }

        private string wildcardValue(string value)
        {
            value = value.Replace("*", "%").Replace("?", "_");
            return value;
        }

        private string escapeForSQLLike(string value)
        {
            char[] escapeChars = new char[] { '%', '_', '^', '[' };
            string escapedValue = "";

            foreach (char c in value)
            {
                if (escapeChars.Contains(c))
                {
                    escapedValue += $"[{c}]";
                }
                else
                {
                    escapedValue += c;
                }
            }
            return escapedValue;
        }
    }
}
