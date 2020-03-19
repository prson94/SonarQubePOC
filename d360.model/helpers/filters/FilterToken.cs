using d360.core.entities;
using System;
using System.Collections.Generic;
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
        private string @operator { get; set; }
        private object value { get; set; }
        private FieldType fieldType { get; set; }
        private string fieldColumn { get; set; }
        private bool isLookupField { get; set; }
        private StringBuilder stringBuilder = new StringBuilder();
        private Dictionary<string, object> sqlParamsRef;

        public bool IsOnlyOperator
        {
            get
            {
                return field == null && value == null;
            }
        }

        public string Field
        {
            get
            {
                return field;
            }
        }

        public FilterToken(ICompanyContext ctx, string field, string op, object value, int? paramIdx = null)
        {
            CompanyContext = ctx;
            parameterIdx = paramIdx ?? -1;
            this.field = field;
            @operator = op;
            this.value = value;
        }

        public string GetSQLForField(ref Dictionary<string, object> sqlParams)
        {
            if (field == null)
            {
                throw new MethodAccessException("Method can be used only when Field Type is loaded. Use LoadFieldType() method before.");
            }
            sqlParamsRef = sqlParams;
            stringBuilder.Clear();
            ValidateTokenForType();
            UpdateTokenValueForType();
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

        public string GetSQLForDefaultField(ref Dictionary<string, object> sqlParams, string fieldSyntax)
        {
            this.sqlParamsRef = sqlParams;
            value = value.ToString().Trim('\'');
            if (this.@operator == "ct")
            {
                value = $"%{value.ToString().Replace("*", "%")}%";
            }

            stringBuilder.Clear();

            stringBuilder.Append(fieldSyntax);
            stringBuilder.Append(GetSQLOperator(@operator));
            stringBuilder.Append($"@filter_{parameterIdx}");

            sqlParamsRef.Add($"@filter_{parameterIdx}", value);
            return stringBuilder.ToString();
        }

        public void LoadFieldType(FieldType ft, List<string> fieldColumns)
        {
            fieldType = ft;
            fieldColumn = fieldColumns.FirstOrDefault(x => x.Contains($"F" + fieldType.ID));
        }

        private void UpdateTokenValueForType()
        {
            switch (fieldType.Type.ToLower())
            {
                case "number":
                    int number = 0;
                    if (!int.TryParse(value.ToString(), out number))
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
                    if (value.ToString() == "0") value = "false";
                    if (value.ToString() == "1") value = "true";
                    if (!bool.TryParse(value.ToString(), out boolean))
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
                        throw new FormatException($"Invalid date value for field '{field}'");
                    }
                    value = date;

                    break;
                default:
                    value = value.ToString().Trim('\'');
                    break;
            }
            if (@operator == "ct")
            {
                value = $"%{value.ToString().Replace("*", "%")}%";
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
                stringBuilder.Append(GetColumnValueSyntax(fieldType.ID));
                stringBuilder.Append(GetSQLOperator(@operator));
                stringBuilder.Append($"@filter_{parameterIdx}");
            }

            sqlParamsRef.Add($"@filter_{parameterIdx}", value);

        }

        private void LoadLookupSql()
        {
            if (fieldType.Type == "Lookup")
            {
                int lookupValue = CompanyContext.GetFieldLookupValue(fieldType.LookupObjectType, fieldType.LookupObjectID.Value, fieldType.ID, value.ToString());
                if (lookupValue <= 0)
                    throw new Exception($"Invalid lookup value '{value}' for field '{field}'");

                value = lookupValue.ToString();

                string condition = "in";
                if (field == "ne")
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

            if (fieldType.Type == "Relationship")
            {
                string condition = "exists";
                if (@operator == "ne")
                {
                    condition = "not exists";
                }

                var whereStatement = $@"{condition}
                                    (select id from intersectdetail where intersecttypeid = {fieldType.LookupObjectID} and subjectuid = a.uid and subjecttypeid = T.ObjectId and subjecttype = T.Object and objectname = @filter_{parameterIdx}
                                    union select id from IntersectDetail where intersecttypeid = {fieldType.LookupObjectID} and objectuid = a.uid and objecttypeid = T.ObjectId and objecttype = T.Object and subjectname = @filter_{parameterIdx})";

                stringBuilder.Append(whereStatement);
            }
        }

        private void ValidateTokenForType()
        {
            bool hasApostrophe = value.ToString().First() == '\'' && value.ToString().Last() == '\'';
            if (!hasApostrophe && !(fieldType.Type == "Number" || fieldType.Type == "Decimal" || fieldType.Type == "Boolean"))
            {
                throw new Exception("Text values should be placed within quotations.");
            }

            if (!IsValidOperatorForFieldType())
            {
                throw new Exception($"Operator '{@operator}' is not valid for '{fieldType.Type}' on field {field}");
            }
        }

        private bool IsValidOperatorForFieldType()
        {
            var operand = @operator.ToLower();
            switch (fieldType.Type.ToLower())
            {
                case "boolean":
                case "lookup":
                case "relationship":
                    return new string[] { "eq", "ne" }.Contains(operand);
                case "number":
                case "decimal":
                case "date":
                case "datetime":
                    return !(new string[] { "ct" }.Contains(operand));
                default:
                    return new string[] { "eq", "ne", "ct" }.Contains(operand);
            }
        }

        private string GetColumnValueSyntax(int fieldTypeId)
        {
            if (fieldColumn == null || fieldColumn.LastIndexOf(" as ") <= 0)
            {
                return $"F{fieldTypeId}.FormattedValue";
            }
            return fieldColumn.Substring(0, fieldColumn.LastIndexOf(" as "));

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
    }
}
