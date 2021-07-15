using d360.core;
using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.helpers.filters
{
    public class FieldToken : FilterBaseToken, IFilterToken
    {
        private bool isLookupField { get; set; }


        public FieldToken(FilterDataProvider fdp, string field, string op, object value, int? paramIdx = null)
        {
            this.dataProvider = fdp;
            parameterIdx = paramIdx ?? -1;
            this.field = field;
            @operator = op;
            this.value = value;

            if (this.value != null && this.value.ToString().ToLower(CultureInfo.InvariantCulture) == "null")
            {
                this.IsNullValue = true;
            }
        }

        public string GetSqlExpression(Dictionary<string, object> sqlParams)
        {
            if (field == null)
            {
                throw new MethodAccessException("Method can be used only when Field Type is loaded. Use LoadFieldType() method before.");
            }
            sqlParamsRef = sqlParams;
            stringBuilder.Clear();
            if (!this.IsNullValue)
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

            string[] lookupFieldTypes = new[] { "Lookup", "Relationship" };

            if (lookupFieldTypes.Select(x => x.ToLower()).Contains(fieldType.Type.ToLower()))
            {
                if (fieldType.LookupObjectID == null)
                {
                    throw new Exception("Lookup field type is missing LookupObjectID value!");
                }
                this.isLookupField = true;
                if (this.IsComplexField)
                {
                    if (@operator == "eq")
                    {
                        @operator = "ct";
                        this.value = "%" + this.value + "%";
                    }

                    if (@operator == "ne")
                    {
                        @operator = "nct";
                        this.value = "%" + this.value + "%";
                    }
                }
                else
                {
                    LoadLookupSql();
                }
            }

            if (!this.isLookupField && fieldType.Type != DataType.Color.ToString())
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

                this.AppendNullOperatorForNotOperators(fieldSql);
            }

            if (fieldType.Type == DataType.Color.ToString())
            {
                if (@operator == "eq")
                {
                    @operator = "ct";
                    value = "%\"Name\":\"" + value.ToString().Trim() + "\"%";
                }

                if (@operator == "ne")
                {
                    @operator = "nct";
                    value = "%\"Name\":\"" + value.ToString().Trim() + "\"%";
                }

                this.field = "ISNULL(ACJ.ColorJson,'')";
            }

            if (sqlParamsRef != null)
            {
                sqlParamsRef.Add($"@filter_{parameterIdx}", value);
            }

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

        public void LoadFieldType(FieldType ft, List<string> fieldColumns)
        {
            fieldType = ft;
            if (fieldColumns != null)
            {
                fieldColumn = fieldColumns.FirstOrDefault(x => x.Contains($"F" + fieldType.ID));
            }
        }

        private void LoadLookupSql()
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

        private void ValidateTokenForType()
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

        private string GetColumnValueSyntax(int fieldTypeId)
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
    }
}
