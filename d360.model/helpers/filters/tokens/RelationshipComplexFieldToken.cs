using d360.core.entities;
using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.helpers.filters
{
    public class RelationshipComplexFieldToken : FilterBaseToken, IFilterToken
    {
        List<FieldType> fieldTypes = new List<FieldType>();

        public RelationshipComplexFieldToken(FilterDataProvider fdp, string field, string op, object value, List<FieldType> types)
        {
            this.dataProvider = fdp;
            this.fieldTypes = types;
            this.field = field.Replace("$related:", "");
            @operator = op;
            this.value = value.ToString().Replace("'", "");

            if (this.value != null && this.value.ToString().ToLower(CultureInfo.InvariantCulture) == "null")
            {
                this.IsNullValue = true;
            }
        }

        public string GetSqlExpression(Dictionary<string, object> sqlParams)
        {
            var intersectTypeUid = this.Field.ToLower().Replace("$related:", "");
            var intersectUid = this.EscapedValueAsString;
            var ftRelationship = fieldTypes.Where(x => x.Name.ToLower() == this.Field.ToLower()).FirstOrDefault();
            var ftQueryName = fieldTypes.FirstOrDefault(x => x.LookupObjectID == ftRelationship.LookupObjectID && x.LookupObjectType == ftRelationship.LookupObjectType && ftRelationship.Name != x.Name).Name;
            var relationshipFilterSQL = "";
            if (ftRelationship != null)
            {
                string sqlOperator = "=";
                string relField = ftQueryName.Replace("_IntersectTypeUid", "_Uid");
                if (IsNullValue)
                {
                    sqlOperator = " is null ";
                    if (@operator == "ne")
                    {
                        sqlOperator = " is not null";
                    }

                    relationshipFilterSQL = $"( {ftQueryName} {sqlOperator})";
                }
                else
                {
                    if (@operator == "ne")
                    {
                        sqlOperator = "<>";
                    }

                    relationshipFilterSQL = $"( {ftQueryName} = '{intersectTypeUid}' and {relField} {sqlOperator} '{intersectUid.Replace("'", "")}')";
                }

            }

            return relationshipFilterSQL;
        }
    }
}
