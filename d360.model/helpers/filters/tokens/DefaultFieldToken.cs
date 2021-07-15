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
        public DefaultFieldToken(FilterDataProvider fdp, string field, string op, object value, DefaultFilter @default, int ? paramIdx = null)
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

            if (!IsValidOperatorForFieldType(filter))
            {
                throw new Exception($"Operator '{@operator}' is not valid for '{filter.SqlFieldType.ToString().ToLower()}' on field {field}");
            }

            if (!this.IsNullValue)
            {
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

                AppendNullOperatorForNotOperators(filter.SqlExpression);
                return stringBuilder.ToString();
            }
            else
            {
                if (!(new[] { "eq", "ne" }.Contains(@operator)))
                {
                    throw new FormatException($"NULL value filter can be used only with 'eq' and 'ne' operator!");
                }
                stringBuilder.Append(filter.SqlExpression);
                stringBuilder.Append(GetSQLNullOperator(@operator));

                return stringBuilder.ToString();
            }



        }

    }
}
