using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.helpers.filters
{
    public class DefaultFieldToken : FilterBaseToken, IFilterToken
    {
        private DefaultFilter filter;
        public DefaultFieldToken(IFilterDataProvider fdp, string field, string op, object value, DefaultFilter @default, int? paramIdx = null)
        {
            this.dataProvider = fdp;
            parameterIdx = paramIdx ?? -1;
            this.field = field;
            @operator = op;
            this.value = value;
            this.filter = @default;

            if (this.value != null && this.value.ToString().ToLower(CultureInfo.InvariantCulture) == "null")
            {
                this.IsNullValue = true;
            }
        }


        public string GetSqlExpression(Dictionary<string, object> sqlParams)
        {
            this.sqlParamsRef = sqlParams;


            if (filter.SqlFieldType == SqlFieldType.Xml)
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
                string formattedSql = "";
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
                        formattedSql = "{0}.exist('/path/segment[1][contains(.,sql:variable(\"{1}\"))]') = 1";
                        break;
                    case "ew":
                        formattedSql = "{0}.exist('/path/segment[last()][contains(.,sql:variable(\"{1}\"))]') = 1";
                        break;
                    case "ct":
                        formattedSql = "{0}.exist('/path/segment[contains(.,sql:variable(\"{1}\"))]') = 1";
                        break;
                    case "nct":
                        formattedSql = "{0}.exist('/path/segment[contains(.,sql:variable(\"{1}\"))]') = 0";
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
                                segmentFilterList.Add("{0}" + $".exist('/path/segment[{i + 1}][.=sql:variable(\"{pName}_{i}\")]') = {resultValue}");
                                sqlParamsRef.Add($"{pName}_{i}", values[i]);
                            }
                            formattedSql = string.Join(" and ", segmentFilterList);
                        }
                        else
                        {
                            formattedSql = "{0}.exist('/path/segment[.=sql:variable(\"{1}\")]') = " + resultValue;
                        }
                        break;
                }
                stringBuilder.AppendFormat(formattedSql, filter.SqlExpression, pName);

                sqlParamsRef.Add(pName, value);
            }
            else
            {
                if (!IsValidOperatorForFieldType(filter))
                {
                    throw new Exception($"Operator '{@operator}' is not valid for '{filter.SqlFieldType.ToString().ToLower()}' on field {field}");
                }

                if (!this.IsNullValue)
                {
                    ValidateTokenForType(filter);
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

                    this.AppendNullOperatorForNotOperators(filter.SqlExpression);
                }
                else
                {
                    if (!(new[] { "eq", "ne" }.Contains(@operator)))
                    {
                        throw new FormatException($"NULL value filter can be used only with 'eq' and 'ne' operator!");
                    }
                    stringBuilder.Append(filter.SqlExpression);
                    stringBuilder.Append(GetSQLNullOperator(@operator));
                }
            }

            return stringBuilder.ToString();
        }
    }
}
