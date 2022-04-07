using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using d360.model.helpers.filters.program;

namespace d360.model.helpers.filters
{
    public class DefaultFieldToken : FilterBaseToken, IFilterToken
    {
        private readonly IFieldValueValidator fieldValueValidator;

        public DefaultFieldToken(IFilterDataProvider fdp, string field, string op, object value, DefaultFilter @default, int? paramIdx = null)
        {
            dataProvider = fdp;
            parameterIdx = paramIdx ?? -1;
            this.field = field;
            @operator = op;
            this.value = value;
            defaultFilter = @default;

            if (this.value != null && this.value.ToString().ToLower(CultureInfo.InvariantCulture) == "null")
            {
                IsNullValue = true;
            }

            fieldValueValidator = GetValueValidator();
        }

        public string GetSqlExpression(Dictionary<string, object> sqlParams)
        {
            sqlParamsRef = sqlParams;
            value = value.ToString().ToLower(CultureInfo.InvariantCulture);

            if (defaultFilter.SqlFieldType == SqlFieldType.Xml)
            {
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

                stringBuilder.AppendFormat(formattedSql, defaultFilter.SqlExpression, pName);
                sqlParamsRef.Add(pName, value);
            }
            else
            {
                if (!FilterHelpers.IsValidOperatorForFieldType(CurrentFieldType, @operator))
                {
                    throw new FilterExpressionParserException($"Operator '{@operator}' is not valid for '{CurrentFieldType}' on field {field}");
                }

                if (!IsNullValue)
                {
                    FilterHelpers.ValidateValueForType(CurrentFieldType, value);

                    var valueValidation = fieldValueValidator.CheckValue(value, field, @operator);
                    
                    if (!valueValidation.Status)
                    {
                        throw new FormatException(valueValidation.Message);
                    }
                    
                    value = valueValidation.UpdatedValue;
                    value = value.ToString().Trim('\'');

                    if (@operator == "ct" || @operator == "nct")
                    {
                        bool isStartWith = value.ToString().Last() == '*';
                        bool isEndWith = value.ToString().First() == '*';
                        bool isBoth = (isStartWith && isEndWith) || (!isStartWith && !isEndWith);

                        if (isBoth)
                        {
                            value = $"%{FilterHelpers.WildcardValue(FilterHelpers.EscapeForSQLLike(value.ToString()))}%";
                        }
                        else
                        {
                            //Wildcard will be present from request
                            value = $"{FilterHelpers.WildcardValue(FilterHelpers.EscapeForSQLLike(value.ToString()))}";
                        }
                    }

                    stringBuilder.Clear();

                    if (ConvertToNvarChar)
                    {
                        defaultFilter.SqlExpression = $"CONVERT(VARCHAR,{defaultFilter.SqlExpression},120)";
                    }

                    stringBuilder.Append(defaultFilter.SqlExpression);
                    stringBuilder.Append(FilterHelpers.GetSQLOperator(@operator));
                    stringBuilder.Append($"@filter_{parameterIdx}");

                    sqlParamsRef.Add($"@filter_{parameterIdx}", value);

                    AppendNullOperatorForNotOperators(defaultFilter.SqlExpression);
                }
                else
                {
                    if (!new[] { "eq", "ne" }.Contains(@operator))
                    {
                        throw new FormatException($"NULL value filter can be used only with 'eq' and 'ne' operator!");
                    }

                    stringBuilder.Append(defaultFilter.SqlExpression);
                    stringBuilder.Append(FilterHelpers.GetSQLNullOperator(@operator));
                }
            }

            return stringBuilder.ToString();
        }
    }
}
