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
    public class FieldToken : FilterBaseToken, IFilterToken
    {
        IFieldValueValidator fieldValueValidator;

        public FieldToken(IFilterDataProvider fdp, string field, string op, object value, int? paramIdx = null)
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
            if (field == null)
            {
                throw new MethodAccessException("Method can be used only when Field Type is loaded. Use LoadFieldType() method before.");
            }
            sqlParamsRef = sqlParams;
            stringBuilder.Clear();
            if (!this.IsNullValue)
            {
                if (!FilterHelpers.IsValidOperatorForFieldType(this.CurrentFieldType, @operator))
                {
                    throw new Exception($"Operator '{@operator}' is not valid for '{this.CurrentFieldType}' on field {field}");
                }

                FilterHelpers.ValidateValueForType(this.CurrentFieldType, value);

                var valueValidation = this.fieldValueValidator.CheckValue(this.value, this.field, this.@operator);
                if (!valueValidation.Status)
                {
                    throw new FormatException(valueValidation.Message);
                }
                this.value = valueValidation.UpdatedValue;

                UpdateTokenValueForType();
            }
            else
            {
                UpdateTokenForNullValue();
            }

            return stringBuilder.ToString();
        }

        private void UpdateTokenForNullValue()
        {
            if (!(new[] { "eq", "ne" }.Contains(@operator)))
            {
                throw new FormatException($"NULL value filter can be used only with 'eq' and 'ne' operator!");
            }
            var fieldSql = GetColumnValueSyntax(fieldType.ID);

            stringBuilder.Append(fieldSql);
            stringBuilder.Append(GetSQLNullOperator(@operator));
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
