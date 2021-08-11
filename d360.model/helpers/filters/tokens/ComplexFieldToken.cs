using d360.core;
using d360.core.entities;
using d360.model.helpers.filters.program;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.helpers.filters
{
    public class ComplexFieldToken : FilterBaseToken, IFilterToken
    {
        IFieldValueValidator fieldValueValidator;
        public ComplexFieldToken(IFilterDataProvider fdp, string field, string op, object value, int? paramIdx = null)
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
            if (!IsNullValue)
            {
                var valueValidation = this.fieldValueValidator.CheckValue(this.value, this.field, this.@operator);
                if (!valueValidation.Status)
                {
                    throw new FormatException(valueValidation.Message);
                }
                this.value = valueValidation.UpdatedValue;

                UpdateTokenValueForType(true);
                return $"( {Field} {GetSQLOperator(@operator)} '{EscapedValueAsString}')";
            }
            else
            {
                if (!(new[] { "eq", "ne" }.Contains(@operator)))
                {
                    throw new FormatException($"NULL value filter can be used only with 'eq' and 'ne' operator!");
                }

                return $"( {Field} { GetSQLNullOperator(@operator)})";
            }
        }

        public void LoadFieldType(FieldType ft, List<string> fieldColumns)
        {
            fieldType = ft;
            if (fieldColumns != null)
            {
                fieldColumn = fieldColumns.FirstOrDefault(x => x.Contains($"F" + fieldType.ID));
            }

            this.fieldValueValidator = GetValueValidator();
        }

    }
}
