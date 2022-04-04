using System.Collections.Generic;
using System.Globalization;

using d360.model.helpers.filters.program;

namespace d360.model.helpers.filters
{
    public class OperatorToken : FilterBaseToken, IFilterToken
    {
        public OperatorToken(IFilterDataProvider fdp, string field, string op, object value, int? paramIdx = null)
        {
            dataProvider = fdp;
            parameterIdx = paramIdx ?? -1;
            this.field = field;
            @operator = op;
            this.value = value;

            if (this.value != null && this.value.ToString().ToLower(CultureInfo.InvariantCulture) == "null")
            {
                IsNullValue = true;
            }
        }

        public string GetSqlExpression(Dictionary<string, object> sqlParams)
        {
            stringBuilder.Clear();

            if (@operator != "(" && @operator != ")")
            {
                stringBuilder.Append(FilterHelpers.GetLogicalOperator(@operator));
            }
            else
            {
                stringBuilder.Append(@operator);
            }

            return stringBuilder.ToString();
        }
    }
}
