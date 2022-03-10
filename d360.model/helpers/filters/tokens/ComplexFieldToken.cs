using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

using d360.core.entities;
using d360.model.helpers.filters.program;

namespace d360.model.helpers.filters
{
    public class ComplexFieldToken : FilterBaseToken, IFilterToken
    {
        private IFieldValueValidator fieldValueValidator;

        public ComplexFieldToken(IFilterDataProvider fdp, string field, string op, object value, int? paramIdx = null)
        {
            dataProvider = fdp;
            parameterIdx = paramIdx ?? -1;
            this.field = field;
            @operator = op;
            this.value = value;

            if (@operator == "ct")
            {
                this.value = HttpUtility.UrlDecode(value.ToString());
            }

            if (this.value != null && this.value.ToString().ToLower(CultureInfo.InvariantCulture) == "null")
            {
                IsNullValue = true;
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
            
            if (fieldType.Type == "Path" && fieldType.Name.Contains("__assetPath"))
            {
                var pathSegmentsLocation = fieldType.Name.Replace("__assetPath", "") + "p.Segments";
                value = value.ToString().ToLower(CultureInfo.InvariantCulture);
                
                if (value.ToString().StartsWith("'"))
                {
                    value = ((string)value).TrimStart('\'');
                }
                
                if (value.ToString().EndsWith("'"))
                {
                    value = ((string)value).TrimEnd('\'');
                }
                
                var values = value.ToString().Split('>').ToList();
                
                for (int i = 0; i < values.Count; i++)
                {
                    values[i] = values[i].Trim();
                }
                
                if (value.ToString().EndsWith("*") && @operator == "ct")
                {
                    value = ((string)value).TrimEnd('*');
                    @operator = "sw";
                }
                
                if (value.ToString().StartsWith("*") && @operator == "ct")
                {
                    value = ((string)value).TrimStart('*');
                    @operator = "ew";
                }

                string pName = $"@filter_{ parameterIdx}";
                string formattedSql;
                switch (@operator)
                {
                    case "ge":
                        formattedSql = "{0}.exist('/path/segment[. >= sql:variable(\"{1}\")]') = 1";
                        break;
                    case "gt":
                        formattedSql = "{0}.exist('/path/segment[. > sql:variable(\"{1}\")]') = 1";
                        break;
                    case "le":
                        formattedSql = "{0}.exist('/path/segment[. <= sql:variable(\"{1}\")]') = 1";
                        break;
                    case "lt":
                        formattedSql = "{0}.exist('/path/segment[. < sql:variable(\"{1}\")]') = 1";
                        break;
                    case "sw":
                        formattedSql = "{0}.exist('/path/segment[1][contains(lower-case(.),sql:variable(\"{1}\"))]') = 1";
                        break;
                    case "ew":
                        formattedSql = "{0}.exist('/path/segment[last()][contains(lower-case(.),sql:variable(\"{1}\"))]') = 1";
                        break;
                    case "ct":
                        formattedSql = "{0}.exist('/path/segment[contains(lower-case(.),sql:variable(\"{1}\"))]') = 1";
                        break;
                    case "nct":
                        formattedSql = "{0}.exist('/path/segment[contains(lower-case(.),sql:variable(\"{1}\"))]') = 0";
                        break;
                    default: //default is eq
                        string resultValue = "1";

                        if (@operator == "ne")
                        {
                            resultValue = "0";
                        }

                        if (values.Count > 1)
                        {
                            var segmentFilterList = new List<string>();
                            for (int i = 0; i < values.Count; i++)
                            {
                                values[i] = values[i].Trim();
                                segmentFilterList.Add("{0}" + $".exist('/path/segment[{i + 1}][lower-case(.)=sql:variable(\"{pName}_{i}\")]') = {resultValue}");
                                sqlParamsRef.Add($"{pName}_{i}", values[i]);
                            }
                            formattedSql = string.Join(" and ", segmentFilterList);
                        }
                        else
                        {
                            formattedSql = "{0}.exist('/path/segment[lower-case(.)=sql:variable(\"{1}\")]') = " + resultValue;
                        }
                        break;
                }
                stringBuilder.AppendFormat(formattedSql, pathSegmentsLocation, pName);

                sqlParamsRef.Add(pName, value);
            }
            else if (!IsNullValue)
            {
                FilterHelpers.ValidateValueForType(CurrentFieldType, value);

                if (@operator != "ct")
                {
                    var valueValidation = fieldValueValidator.CheckValue(value, field, @operator);
                    
                    if (!valueValidation.Status)
                    {
                        throw new FormatException(valueValidation.Message);
                    }

                    value = valueValidation.UpdatedValue;
                }
                else
                {
                    value = value.ToString().Trim('\'');
                }

                UpdateTokenValueForType(complexField: true);
            }
            else
            {
                UpdateTokenForNullValue();
            }

            return stringBuilder.ToString();
        }

        private void UpdateTokenForNullValue()
        {
            if (!new[] { "eq", "ne" }.Contains(@operator))
            {
                throw new FormatException($"NULL value filter can be used only with 'eq' and 'ne' operator!");
            }

            var fieldSql = GetColumnValueSyntax(fieldType.ID);

            stringBuilder.Append(fieldSql);
            stringBuilder.Append(FilterHelpers.GetSQLNullOperator(@operator));
        }

        public void LoadFieldType(FieldType ft, IReadOnlyList<string> fieldColumns)
        {
            fieldType = ft;

            if (fieldColumns != null)
            {
                fieldColumn = fieldColumns.FirstOrDefault(x => x.Contains($"F" + fieldType.ID));
            }

            if (fieldColumn == null && fieldColumns != null)
            {
                fieldColumn = fieldColumns.FirstOrDefault(x => x.ToLowerInvariant().Contains($"[{fieldType.Name.ToLowerInvariant()}]"));
            }

            fieldValueValidator = GetValueValidator();
        }
    }
}
