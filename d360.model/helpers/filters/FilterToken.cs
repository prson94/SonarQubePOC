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


        private int _parameterIdx { get; set; }
        private string _field { get; set; }
        private string _operator { get; set; }
        private object _value { get; set; }
        private FieldType _fieldType { get; set; }
        private string _fieldColumn { get; set; }
        private bool _isLookupField { get; set; }
        private StringBuilder _stringBuilder = new StringBuilder();
        private Dictionary<string, object> _sqlParams;

        public bool IsOnlyOperator
        {
            get
            {
                return _field == null && _value == null;
            }
        }

        public string Field
        {
            get
            {
                return _field;
            }
        }

        public FilterToken(ICompanyContext ctx, string field, string op, object value, int? paramIdx = null)
        {
            CompanyContext = ctx;
            _parameterIdx = paramIdx ?? -1;
            _field = field;
            _operator = op;
            _value = value;
        }

        public string GetSQLForField(ref Dictionary<string, object> sqlParams)
        {
            if (_field == null)
            {
                throw new MethodAccessException("Method can be used only when Field Type is loaded. Use LoadFieldType() method before.");
            }
            _sqlParams = sqlParams;
            _stringBuilder.Clear();
            ValidateTokenForType();
            UpdateTokenValueForType();
            return _stringBuilder.ToString();
        }

        public string GetSQLForOperator()
        {
            if (!IsOnlyOperator)
            {
                throw new MethodAccessException("Method can be used only for non field tokens");
            }
            _stringBuilder.Clear();
            if (_operator != "(" && _operator != ")")
            {
                _stringBuilder.Append(GetLogicalOperator(_operator));
            }
            else
            {
                _stringBuilder.Append(_operator);
            }
            return _stringBuilder.ToString();
        }

        public string GetSQLForDefaultField(ref Dictionary<string, object> sqlParams, string fieldSyntax)
        {
            _sqlParams = sqlParams;
            _value = _value.ToString().Trim('\'');
            if (_operator == "ct")
            {
                _value = $"%{_value.ToString().Replace("*", "%")}%";
            }

            _stringBuilder.Clear();

            _stringBuilder.Append(fieldSyntax);
            _stringBuilder.Append(GetSQLOperator(_operator));
            _stringBuilder.Append($"@filter_{_parameterIdx}");

            _sqlParams.Add($"@filter_{_parameterIdx}", _value);
            return _stringBuilder.ToString();
        }

        public void LoadFieldType(FieldType ft, List<string> fieldColumns)
        {
            _fieldType = ft;
            _fieldColumn = fieldColumns.FirstOrDefault(x => x.Contains($"F" + _fieldType.ID));
        }

        private void UpdateTokenValueForType()
        {
            switch (_fieldType.Type.ToLower())
            {
                case "number":
                    int number = 0;
                    if (!int.TryParse(_value.ToString(), out number))
                    {
                        throw new FormatException($"Invalid numeric value for field '{_field}'");
                    }
                    _value = number;
                    break;
                case "decimal":
                    decimal dnumber = 0;
                    if (!decimal.TryParse(_value.ToString(), out dnumber))
                    {
                        throw new FormatException($"Invalid decimal value for field '{_field}'");
                    }
                    _value = dnumber;
                    break;
                case "boolean":
                    bool boolean = false;
                    if (_value.ToString() == "0") _value = "false";
                    if (_value.ToString() == "1") _value = "true";
                    if (!bool.TryParse(_value.ToString(), out boolean))
                    {
                        throw new FormatException($"Invalid boolean value for field '{_field}'");
                    }
                    _value = boolean;
                    break;
                case "date":
                case "datetime":
                    DateTime date = new DateTime();
                    if (!DateTime.TryParse(_value.ToString().Trim('\''), out date))
                    {
                        throw new FormatException($"Invalid date value for field '{_field}'");
                    }
                    _value = date;

                    break;
                default:
                    _value = _value.ToString().Trim('\'');
                    break;
            }
            if (_operator == "ct")
            {
                _value = $"%{_value.ToString().Replace("*", "%")}%";
            }

            string[] lookupFieldTypes = new string[] { "Lookup", "Relationship" };

            if (lookupFieldTypes.Select(x => x.ToLower()).Contains(_fieldType.Type.ToLower()) && _fieldType.LookupObjectID != null)
            {
                this._isLookupField = true;
                LoadLookupSql();
            }

            if (!this._isLookupField)
            {
                _stringBuilder.Append(GetColumnValueSyntax(_fieldType.ID));
                _stringBuilder.Append(GetSQLOperator(_operator));
                _stringBuilder.Append($"@filter_{_parameterIdx}");
            }

            _sqlParams.Add($"@filter_{_parameterIdx}", _value);

        }

        private void LoadLookupSql()
        {
            if (_fieldType.Type == "Lookup")
            {
                int lookupValue = CompanyContext.Query<int>(@"select value
  from[dbo].[FieldLookupValue]
  where LookupObjectType = @obj and LookupObjectID = @objId and FieldTypeID = @f and Text = @value",

new { obj = _fieldType.LookupObjectType, objId = _fieldType.LookupObjectID, f = _fieldType.ID, value = _value }).FirstOrDefault();
                if (lookupValue <= 0)
                    throw new Exception($"Invalid lookup value '{_value}' for field '{_field}'");

                _value = lookupValue.ToString();

                string condition = "in";
                if (_field == "ne")
                {
                    condition = "not in";
                }

                if (!string.IsNullOrEmpty(_fieldType.DefaultValue))
                {
                    _stringBuilder.Append($"@filter_{_parameterIdx} {condition} (select * from string_split(coalesce(F{_fieldType.ID}.Value,@defLookupValue{_parameterIdx}),','))");
                    _sqlParams.Add($"@defLookupValue{_parameterIdx}", _fieldType.DefaultValue);
                }
                else
                {
                    _stringBuilder.Append($"@filter_{_parameterIdx} {condition} (select * from string_split(F{_fieldType.ID}.Value,','))");
                }
            }

            if (_fieldType.Type == "Relationship")
            {
                string condition = "exists";
                if (_operator == "ne")
                {
                    condition = "not exists";
                }

                var whereStatement = $@"{condition}
                                    (select id from intersectdetail where intersecttypeid = {_fieldType.LookupObjectID} and subjectuid = a.uid and subjecttypeid = T.ObjectId and subjecttype = T.Object and objectname = @filter_{_parameterIdx}
                                    union select id from IntersectDetail where intersecttypeid = {_fieldType.LookupObjectID} and objectuid = a.uid and objecttypeid = T.ObjectId and objecttype = T.Object and subjectname = @filter_{_parameterIdx})";

                _stringBuilder.Append(whereStatement);
            }
        }

        private void ValidateTokenForType()
        {
            bool hasApostrophe = _value.ToString().First() == '\'' && _value.ToString().Last() == '\'';
            if (!hasApostrophe && !(_fieldType.Type == "Number" || _fieldType.Type == "Decimal" || _fieldType.Type == "Boolean"))
            {
                throw new Exception("Text values should be placed within quotations.");
            }

            if (!IsValidOperatorForFieldType())
            {
                throw new Exception($"Operator '{_operator}' is not valid for '{_fieldType.Type}' on field {_field}");
            }
        }

        private bool IsValidOperatorForFieldType()
        {
            var operand = _operator.ToLower();
            switch (_fieldType.Type.ToLower())
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
            if (_fieldColumn == null || _fieldColumn.LastIndexOf(" as ") <= 0)
            {
                return $"F{fieldTypeId}.FormattedValue";
            }
            return _fieldColumn.Substring(0, _fieldColumn.LastIndexOf(" as "));

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
