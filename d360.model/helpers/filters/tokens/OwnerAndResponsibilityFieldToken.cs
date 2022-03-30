using System.Collections.Generic;
using System.Globalization;

namespace d360.model.helpers.filters
{
    public class OwnerAndResponsibilityFieldToken : FilterBaseToken, IFilterToken 
    {
        public OwnerAndResponsibilityFieldToken(IFilterDataProvider fdp, string field, string op, object value, int? paramIdx = null)
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
            sqlParamsRef = sqlParams;
            stringBuilder.Clear();
            var valueStr = value.ToString().Trim('\'');
            var owner = valueStr.Substring(0, 36);
            var responsibility = valueStr.Substring(37);
            sqlParamsRef.Add($"@filter_{parameterIdx}", owner);
            sqlParamsRef.Add($"@filter_res_{parameterIdx}", responsibility);

            string querySql = $@"EXISTS(
                                            SELECT 1 
                                            FROM 
                                                [dbo].[ResponsibilityDetail] rd 
                                            WHERE 
                                                rd.SecurityAssetUid = @filter_{parameterIdx}
                                                and 
                                                rd.ResponsibilityTypeId = @filter_res_{parameterIdx}
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

            if (@operator == "ne")
            {
                querySql = " NOT " + querySql;
            }

            return querySql;
        }
    }
}
