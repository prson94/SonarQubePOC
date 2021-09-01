using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.helpers.filters
{
    public class RelationFieldToken : FilterBaseToken, IFilterToken
    {
        public RelationFieldToken(FilterDataProvider fdp, string field, string op, object value, int? paramIdx = null)
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
            this.sqlParamsRef = sqlParams;
            stringBuilder.Clear();
            var origQuery = $"{this.field} {this.@operator} {this.value.ToString().Trim('\'')}";

            var filterExpressionParser = new FilterExpressionParser(this.dataProvider, FilterExpressionParseType.Relationships);
            var query = filterExpressionParser.Parse(origQuery.Replace("$related:", ""), out sqlParams, out _);

            foreach (var item in sqlParams)
            {
                string updatedKey = item.Key + "_" + parameterIdx;
                query = query.Replace(item.Key, updatedKey);
                this.sqlParamsRef.Add(updatedKey, item.Value);
            }
            return query;
        }
    }
}
