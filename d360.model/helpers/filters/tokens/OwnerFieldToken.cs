using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.helpers.filters
{
    public class OwnerFieldToken : FilterBaseToken, IFilterToken
    {
        public OwnerFieldToken(IFilterDataProvider fdp, string field, string op, object value, int? paramIdx = null)
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
            var valueStr = this.value.ToString().Trim('\'');
            sqlParamsRef.Add($"@filter_{parameterIdx}", valueStr);

            string querySql = $@"EXISTS(
                                            SELECT 1 
                                            FROM 
                                                [dbo].[ResponsibilityDetail] rd 
                                            WHERE 
                                                rd.SecurityAssetUid = @filter_{parameterIdx}
                                                and 
                                                a.ID=rd.AssetID 
                                                and
                                                rd.isVisible = 1
                                            UNION
                                            SELECT 1 
                                            FROM 
                                                [dbo].[ResponsibilityDetail] rd 
                                            WHERE 
                                                rd.SecurityAssetUid = @filter_{parameterIdx} 
                                                and 
                                                rd.ApplyToType = 1 
                                                and 
                                                rd.AssetID = 0 
                                                and 
                                                rd.AssetTypeId=a.AssetTypeId
                                                and
                                                rd.isVisible = 1
                                            )";

            if (this.@operator == "ne")
            {
                querySql = " NOT " + querySql;
            }

            return querySql;
        }
    }
}
