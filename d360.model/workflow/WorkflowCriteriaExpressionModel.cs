using d360.core.enums.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace d360.model.workflow
{
    internal class WorkflowCriteriaExpressionModel
    {
        public object Value { get; set; }
        public int FieldTypeId { get; set; }
        public CriteriaOperator Operator { get; set; }
        public CriteriaValueDataType ValueDataType { get; set; }
        public string FormInputId { get; set; }
        public int VersionStepId { get; set; }


        public static WorkflowCriteriaExpressionModel Parse(XElement element)
        {
            var dataType = dataTypeFromString((string)element.Attribute("ValueType"));

            return new WorkflowCriteriaExpressionModel
            {
                FieldTypeId = int.Parse(((string)element.Attribute("FieldTypeID") ?? "0")),
                Operator = operatorFromString((string)element.Attribute("Operator")),
                ValueDataType = dataType,
                Value = valueFromString(dataType, (string)element.Attribute("Value")),
                VersionStepId = int.Parse(((string)element.Attribute("VersionStepID") ?? "0")),
                FormInputId = ((string)element.Attribute("FormInputID"))
            };
        }

        public string ToPlainText(CompanyContext context)
        {
            var fieldName = getFieldName(context);
            var operatorText = getOperatorText();

            return $"{fieldName} {operatorText} {Value.ToString()}";
        }

        private string getOperatorText()
        {
            switch (this.Operator)
            {                
                case CriteriaOperator.GreaterThan:
                    return ">";
                case CriteriaOperator.GreaterThanOrEqual:
                    return ">=";
                case CriteriaOperator.LessThan:
                    return "<";
                case CriteriaOperator.LessThanOrEqual:
                    return "<=";
                case CriteriaOperator.Equal:
                    return "=";
                case CriteriaOperator.NotEqual:
                    return "!=";
            }
            return "?";
        }

        protected string getFieldName(CompanyContext context)
        {
            var field = context.FieldTypes.Where(x => x.ID == this.FieldTypeId).FirstOrDefault();

            if (field == null) return "(unknown)";

            return field.FriendlyName;
        }

        private static CriteriaValueDataType dataTypeFromString(string val)
        {
            switch ((val ?? "").ToUpper())
            {
                case "D":
                    return CriteriaValueDataType.Double;
                case "B":
                    return CriteriaValueDataType.Boolean;
                case "T":
                    return CriteriaValueDataType.String;
                case "L":
                    return CriteriaValueDataType.Lookup;
                case "DT":
                    return CriteriaValueDataType.Date;
            }

            return CriteriaValueDataType.Invalid;
        }

        private static CriteriaOperator operatorFromString(string val)
        {
            switch ((val??"").ToUpper())
            {
                case "=":
                    return CriteriaOperator.Equal;
                case ">=":
                    return CriteriaOperator.GreaterThanOrEqual;
                case ">":
                    return CriteriaOperator.GreaterThan;
                case "<=":
                    return CriteriaOperator.LessThanOrEqual;
                case "<":
                    return CriteriaOperator.LessThan;
                case "!=":
                    return CriteriaOperator.NotEqual;         
            }

            return CriteriaOperator.Invalid;
        }

        private static object valueFromString(CriteriaValueDataType type, string val)
        {
            switch (type)
            {
                case CriteriaValueDataType.Invalid:
                    return val;
                case CriteriaValueDataType.Boolean:
                    return (val ?? "").ToUpper() == bool.TrueString.ToUpper() ? true : false;                    
                case CriteriaValueDataType.String:
                    return (val??"").Trim().ToUpper();                    
                case CriteriaValueDataType.Integer:                    
                case CriteriaValueDataType.Double:
                    double dVal = 0;
                    double.TryParse(val, out dVal);
                    return dVal;              
                case CriteriaValueDataType.Date:
                    return int.Parse(val);                    
                case CriteriaValueDataType.Lookup:
                    return int.Parse(val);                
            }

            throw new Exception("ERROR - INVALID DATA TYPE SPECIFIED TO PARSE VALUE");
        }


        public bool IsValueMatch(string givenValue)
        {            
            // dates are number of days from the date field value
            if(this.ValueDataType == CriteriaValueDataType.Date)
            {
                DateTime dt = DateTime.Parse(givenValue);
                DateTime currentDate = DateTime.UtcNow;

                if (Operator == CriteriaOperator.Equal)
                    return ((currentDate - dt).Days == (int)Value);
                else if (Operator == CriteriaOperator.NotEqual)
                    return ((currentDate - dt).Days != (int)Value);
                else if(Operator == CriteriaOperator.GreaterThan)
                    return ((currentDate - dt).Days > (int)Value);
                else if (Operator == CriteriaOperator.GreaterThanOrEqual)
                    return ((currentDate - dt).Days >= (int)Value);
                else if (Operator == CriteriaOperator.LessThan)
                    return ((currentDate - dt).Days < (int)Value);
                else if (Operator == CriteriaOperator.LessThanOrEqual)
                    return ((currentDate - dt).Days <= (int)Value);
                throw new Exception("INVALID DATE OPERATION");
            }
            
            var val = valueFromString(this.ValueDataType, givenValue);

            switch (Operator)
            {                
                case CriteriaOperator.GreaterThan:
                    return isGreaterThan(val);
                case CriteriaOperator.GreaterThanOrEqual:
                    return isGreaterThanOrEqual(val);
                case CriteriaOperator.LessThan:
                    return isLessThan(val);                    
                case CriteriaOperator.LessThanOrEqual:
                    return isLessThanOrEqual(val);
                case CriteriaOperator.Equal:
                    return isEqual(val);
                case CriteriaOperator.NotEqual:
                    return isNotEqual(val);
            }
            
            throw new Exception("INVALID COMPARISON OPERATION");
        }

        private bool isLessThan(object val)
        {
            switch (ValueDataType)
            {                
                case CriteriaValueDataType.Integer:
                    return (int)val < (int)Value;                    
                case CriteriaValueDataType.Double:
                    return (double)val < (double)Value;
            }

            throw new Exception("ERROR - INVALID OPERATION FOR SPECIFIED DATA TYPE.");
        }

        private bool isGreaterThan(object val)
        {
            switch (ValueDataType)
            {
                case CriteriaValueDataType.Integer:
                    return (int)val > (int)Value;
                case CriteriaValueDataType.Double:
                    return (double)val > (double)Value;
            }

            throw new Exception("ERROR - INVALID OPERATION FOR SPECIFIED DATA TYPE.");
        }

        private bool isLessThanOrEqual(object val)
        {
            switch (ValueDataType)
            {
                case CriteriaValueDataType.Integer:
                    return (int)val < (int)Value;
                case CriteriaValueDataType.Double:
                    return (double)val < (double)Value;
            }

            throw new Exception("ERROR - INVALID OPERATION FOR SPECIFIED DATA TYPE.");
        }

        private bool isGreaterThanOrEqual(object val)
        {
            switch (ValueDataType)
            {
                case CriteriaValueDataType.Integer:
                    return (int)val >= (int)Value;
                case CriteriaValueDataType.Double:
                    return (double)val >= (double)Value;
            }

            throw new Exception("ERROR - INVALID OPERATION FOR SPECIFIED DATA TYPE.");
        }

        private bool isEqual(object val)
        {
            switch (ValueDataType)
            {
                case CriteriaValueDataType.Boolean:
                    return (bool)val == (bool)Value;
                case CriteriaValueDataType.String:
                    return String.Compare((string)val,(string)Value, true) == 0;
                case CriteriaValueDataType.Integer:
                    return (int)val == (int)Value;
                case CriteriaValueDataType.Double:
                    return (double)val == (double)Value;
                case CriteriaValueDataType.Lookup:
                    return (int)val == (int)Value;                    
            }

            throw new Exception("ERROR - INVALID OPERATION FOR SPECIFIED DATA TYPE.");
        }

        private bool isNotEqual(object val)
        {
            switch (ValueDataType)
            {
                case CriteriaValueDataType.Boolean:
                    return (bool)val != (bool)Value;
                case CriteriaValueDataType.String:
                    return String.Compare((string)val, (string)Value, true) != 0;
                case CriteriaValueDataType.Integer:
                    return (int)val != (int)Value;
                case CriteriaValueDataType.Double:
                    return (double)val != (double)Value;
                case CriteriaValueDataType.Lookup:
                    return (int)val != (int)Value;
            }

            throw new Exception("ERROR - INVALID OPERATION FOR SPECIFIED DATA TYPE.");
        }
    }
}
