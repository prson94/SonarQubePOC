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

        public void LoadFieldType(FieldType ft, List<string> fieldColumns)
        {
            fieldType = ft;
            if (fieldColumns != null)
            {
                fieldColumn = fieldColumns.FirstOrDefault(x => x.Contains($"F" + fieldType.ID));
            }
        }
    }
}
