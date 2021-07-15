using d360.core.entities;
using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.helpers.filters
{
    public abstract class FilterBaseToken
    {
        protected FilterDataProvider dataProvider;

        protected int parameterIdx { get; set; }
        protected string field { get; set; }
        public string @operator { get; set; }
        protected object value { get; set; }
        public bool IsNullValue { get; set; }
        protected FieldType fieldType { get; set; }
        protected string fieldColumn { get; set; }
        protected bool isLookupField { get; set; }
        protected StringBuilder stringBuilder = new StringBuilder();
        protected Dictionary<string, object> sqlParamsRef;
        protected bool convertToNVarChar = false;

        protected AssetType assetType { get; set; }
        protected IntersectType intersectType { get; set; }

        public bool IsComplexField { get; set; }

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

        public string EscapedValueAsString
        {
            get
            {
                return value.ToString().Replace("'", "''");
            }
        }


        protected void UpdateTokenForNullValue()
        {
            if (!(new[] { "eq", "ne" }.Contains(@operator)))
            {
                throw new FormatException($"NULL value filter can be used only with 'eq' and 'ne' operator!");
            }
            var fieldSql = GetColumnValueSyntax(fieldType.ID);

            stringBuilder.Append(fieldSql);
            stringBuilder.Append(GetSQLNullOperator(@operator));
        }

        protected void CheckFieldValue(DefaultFilter filter = null)
        {
            string ft = filter == null ? fieldType.Type : filter.SqlFieldType.ToString();
            switch (ft.ToLower())
            {
                case "number":
                case "counter":
                    int number = 0;
                    if (!int.TryParse(value.ToString(), NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out number))
                    {
                        //parsing of thousands seperator fails on - symbol
                        if (!int.TryParse(value.ToString(), out number))
                        {
                            throw new FormatException($"Invalid numeric value for field '{field}'");
                        }
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

                        if (filter != null && @operator == "le" && ft.ToLower(CultureInfo.InvariantCulture) == "datetime"
                            && (filter.ApiName == "CreatedOn" || filter.ApiName == "UpdatedOn"))
                        {
                            //CreatedOn and UpdatedOn system fields are DateTime, but UI filtering is treating them as
                            //date fields. In case of "Less or Equal" we need to update date to take into account equal dates
                            date = date.AddHours(23);
                            date = date.AddMinutes(59);
                            date = date.AddSeconds(59);
                            date = date.AddMilliseconds(999);
                            value = date;
                        }
                    }

                    break;
                case "assettypeclass":
                    var classes = AssetTypeClass.BusinessAsset.GetAsList();
                    var match = classes.FirstOrDefault(x => x.Name.ToLower(CultureInfo.InvariantCulture) == value.ToString().ToLower(CultureInfo.InvariantCulture).Trim('\'')
                    || x.Value.ToLower(CultureInfo.InvariantCulture) == value.ToString().ToLower(CultureInfo.InvariantCulture).Trim('\''));

                    if (match == null)
                    {
                        throw new FormatException($"Invalid AssetTypeClass value for field '{field}'");
                    }

                    value = (int)match.ID;
                    break;
                default:


                    value = value.ToString().Trim('\'').Replace("&apos;", "'");
                    break;
            }
        }

        protected void LoadLookupSql()
        {
            bool isFieldFromRel = this.dataProvider.IsFieldFromRelationship(fieldType.ID);

            string type = fieldType.Type;
            int fieldTypeId = fieldType.ID;
            int fieldTypeIdForLookupValue = fieldType.ID;
            string lookupObjectType = fieldType.LookupObjectType;
            int lookupObjectId = fieldType.LookupObjectID.HasValue ? fieldType.LookupObjectID.Value : 0;
            string defaultValue = fieldType.DefaultValue;
            bool allowAllValue = fieldType.AllowAllValue;
            string valueQueryPart = "Value";

            //handle field from relationship list values
            if (isFieldFromRel)
            {
                var lookupFieldType = this.dataProvider.GetFieldTypeById(fieldType.LookupObjectFieldTypeID);
                fieldTypeIdForLookupValue = lookupFieldType.ID;
                lookupObjectType = lookupFieldType.LookupObjectType;
                lookupObjectId = lookupFieldType.LookupObjectID.HasValue ? lookupFieldType.LookupObjectID.Value : 0;
                defaultValue = lookupFieldType.DefaultValue;
                allowAllValue = lookupFieldType.AllowAllValue;
                valueQueryPart = "FormattedValue";
            }

            if (type == "Lookup")
            {
                if (@operator == "ct")
                {
                    stringBuilder.Append($"F{fieldTypeId}.FormattedValue like @filter_{parameterIdx}");
                }
                else
                {

                    int lookupValue = this.dataProvider.GetFieldLookupValue(lookupObjectType, lookupObjectId, fieldTypeIdForLookupValue, value.ToString());
                    if (lookupValue <= 0)
                        throw new Exception($"Invalid lookup value '{value}' for field '{field}'");

                    if (!isFieldFromRel)
                    {
                        value = lookupValue.ToString();
                    }


                    string condition = "in";
                    if (@operator == "ne")
                    {
                        condition = "not in";
                    }
                    var basicSqlExpression = string.Empty;

                    if (!string.IsNullOrEmpty(defaultValue))
                    {
                        basicSqlExpression = $"@filter_{parameterIdx} {condition} (select * from string_split(coalesce(F{fieldTypeId}.{valueQueryPart},@defLookupValue{parameterIdx}),','))";
                        sqlParamsRef.Add($"@defLookupValue{parameterIdx}", defaultValue);
                    }
                    else
                    {
                        basicSqlExpression = $"@filter_{parameterIdx} {condition} (select * from string_split(F{fieldTypeId}.{valueQueryPart},','))";
                    }

                    if (allowAllValue)
                    {
                        basicSqlExpression = $"(F{fieldTypeId}.{valueQueryPart} = '0' or {basicSqlExpression})";
                    }
                    stringBuilder.Append(basicSqlExpression);

                }
            }

            if (type == "Relationship")
            {
                string condition = "exists";
                if (@operator == "ne")
                {
                    condition = "not exists";
                }

                var whereStatement = $@"{condition}
                                    (select id from intersectdetail where intersecttypeid = {lookupObjectId} and subjectuid = a.uid and subjecttypeid = T.ObjectId and subjecttype = T.Object and objectname {(@operator == "ct" ? "like" : "=")} @filter_{parameterIdx}
                                    union select id from IntersectDetail where intersecttypeid = {lookupObjectId} and objectuid = a.uid and objecttypeid = T.ObjectId and objecttype = T.Object and subjectname {(@operator == "ct" ? "like" : "=")} @filter_{parameterIdx})";

                stringBuilder.Append(whereStatement);
            }
        }

        protected void ValidateTokenForType()
        {
            bool hasApostrophe = value.ToString().First() == '\'' && value.ToString().Last() == '\'';
            if (!hasApostrophe && !(fieldType.Type == "Number" || fieldType.Type == "Decimal" || fieldType.Type == "Boolean" || fieldType.Type == "Score" || fieldType.Type == "Counter"))
            {
                throw new Exception("Text values should be placed within quotations.");
            }

            if (!IsValidOperatorForFieldType())
            {
                throw new Exception($"Operator '{@operator}' is not valid for '{fieldType.Type}' on field {field}");
            }
        }

        protected bool IsValidOperatorForFieldType(DefaultFilter defaultFilter = null)
        {
            var operand = @operator.ToLower();
            string fType = defaultFilter == null ? fieldType.Type.ToLower() : defaultFilter.SqlFieldType.ToString().ToLower();

            switch (fType)
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
                    return true;
                case "assettypeclass":
                    return new[] { "eq", "ne" }.Contains(operand);
                default:
                    return new[] { "eq", "ne", "ct", "nct" }.Contains(operand);
            }
        }

        protected string GetColumnValueSyntax(int fieldTypeId)
        {
            if (fieldType.Type == "Path")
            {
                return $"Node.DisplayPath";
            }
            else if (fieldType.Type == "Counter")
            {
                return $"F{fieldType.ID}.FormattedValue";
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

        protected string GetSQLOperator(string value)
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

        protected string GetSQLNullOperator(string value)
        {
            switch (value)
            {
                case "eq": return " is null";
                case "ne": return " is not null";
                default: throw new Exception($"Invalid comparison operator '{value}'");
            }
        }

        protected SplitFilterCriteriaRelationship GetSplitFilterCriteriaRelationship()
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

        protected string wildcardValue(string value)
        {
            value = value.Replace("*", "%").Replace("?", "_");
            return value;
        }

        protected string escapeForSQLLike(string value)
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

        protected void AppendNullOperatorForNotOperators(string fieldName)
        {
            if (this.@operator == "ne" || this.@operator == "nct")
            {
                stringBuilder.Insert(0, "(");
                stringBuilder.Append(" or ");
                stringBuilder.Append(fieldName);
                stringBuilder.Append(" is null");
                stringBuilder.Append(")");
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
        Text, Boolean, Number, Decimal, Date, DateTime, Guid, AssetTypeClass
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
