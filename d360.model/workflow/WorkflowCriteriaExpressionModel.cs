using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

using d360.core.enums.Workflow;
using d360.core.types;

namespace d360.model.workflow
{
    //TODO: should be refactored to service + model
    internal class WorkflowCriteriaExpressionModel
    {
        //TODO: this should be injected to service
        private readonly IDateTimeService DateTimeService = new DateTimeService();

        public object Value { get; set; }
        
        public int FieldTypeId { get; set; }
        
        public CriteriaOperator Operator { get; set; }
        
        public CriteriaValueDataType ValueDataType { get; set; }
        
        public CriteriaConnector CriteriaConnector { get; set; }
        
        public string FormInputId { get; set; }
        
        public int VersionStepId { get; set; }
        
        public string ContextualFieldID { get; set; }
        
        public bool IsCriteriaChecked { get; set; }

        public static WorkflowCriteriaExpressionModel Parse(XElement element)
        {
            CriteriaValueDataType dataType = dataTypeFromString((string)element.Attribute("ValueType"));
            CriteriaOperator @operator = operatorFromString((string)element.Attribute("Operator"));
            CriteriaConnector @connector = criteriaConnectorFromString((string)element.Attribute("Connector"));

            List<CriteriaOperator> noValueOperators = new List<CriteriaOperator>()
            {
                CriteriaOperator.Changed,
                CriteriaOperator.Populated,
                CriteriaOperator.NotPopulated
            };

            return new WorkflowCriteriaExpressionModel
            {
                FieldTypeId = int.Parse((string)element.Attribute("FieldTypeID") ?? "0"),
                ContextualFieldID = (string)element.Attribute("ContextualFieldID") ?? "",
                Operator = @operator,
                ValueDataType = dataType,
                Value = noValueOperators.Contains(@operator) ? "" : valueFromString(dataType, (string)element.Attribute("Value")),
                VersionStepId = int.Parse((string)element.Attribute("VersionStepID") ?? "0"),
                FormInputId = (string)element.Attribute("FormInputID"),
                CriteriaConnector = connector
            };
        }

        public string ToPlainText(ICompanyContext context)
        {
            string fieldName = getFieldName(context);
            string operatorText = getOperatorText();

            return $"{fieldName} {operatorText} {Value}";
        }

        private string getOperatorText()
        {
            switch (Operator)
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

        protected string getFieldName(ICompanyContext context)
        {
            core.entities.FieldType field = context.FieldTypes.Where(x => x.ID == FieldTypeId).FirstOrDefault();

            if (field == null)
            {
                return "(unknown)";
            }

            return field.FriendlyName;
        }

        private static CriteriaConnector criteriaConnectorFromString(string val)
        {
            if (!string.IsNullOrEmpty(val))
            {
                switch (val.ToUpper())
                {
                    case "AND":
                        return CriteriaConnector.AND;
                    case "OR":
                        return CriteriaConnector.OR;
                }
            }

            return CriteriaConnector.AND;
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
            switch ((val ?? "").ToUpper())
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
                case "C":
                    return CriteriaOperator.Changed;
                case "P":
                    return CriteriaOperator.Populated;
                case "NP":
                    return CriteriaOperator.NotPopulated;
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

                    if (string.IsNullOrEmpty(val))
                    {
                        return null;
                    }

                    return (val ?? "").ToUpper() == bool.TrueString.ToUpper();
                case CriteriaValueDataType.String:
                    return (val ?? "").Trim().ToUpper();
                case CriteriaValueDataType.Integer:
                case CriteriaValueDataType.Double:
                    double? dVal = null;

                    if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out double dValParsed))
                    {
                        dVal = dValParsed;
                    }

                    return dVal;
                case CriteriaValueDataType.Date:

                    if (string.IsNullOrEmpty(val))
                    {
                        return null;
                    }

                    return int.Parse(val);
                case CriteriaValueDataType.Lookup:
                    {
                        //multiselect
                        if (!string.IsNullOrEmpty(val) && val.Contains(","))
                        {
                            return int.MaxValue;
                        }

                        if (int.TryParse(val, out int res))
                        {
                            return res;
                        }
                        else
                        {
                            return -1;
                        }
                    }
            }

            throw new Exception("ERROR - INVALID DATA TYPE SPECIFIED TO PARSE VALUE");
        }

        public bool IsValueMatch(string givenValue)
        {
            // dates are number of days from the date field value
            if (ValueDataType == CriteriaValueDataType.Date)
            {
                if (Operator == CriteriaOperator.NotPopulated)
                {
                    return string.IsNullOrEmpty(givenValue) || !DateTimeService.CanParse(givenValue);
                }

                if (Operator == CriteriaOperator.Populated)
                {
                    return DateTimeService.CanParse(givenValue);
                }

                if (!DateTimeService.TryParse(givenValue, out DateTime parsedDateTime))
                {
                    return false;
                }

                DateTimeOffset currentDate = DateTimeService.Now();

                int numDays = Convert.ToInt32((parsedDateTime.Date - currentDate.Date).TotalDays);
                int value = (int)Value;

                if (Operator == CriteriaOperator.Equal)
                {
                    return numDays == value;
                }
                else if (Operator == CriteriaOperator.NotEqual)
                {
                    return numDays != value;
                }
                else if (Operator == CriteriaOperator.GreaterThan)
                {
                    return numDays > value;
                }
                else if (Operator == CriteriaOperator.GreaterThanOrEqual)
                {
                    return numDays >= value;
                }
                else if (Operator == CriteriaOperator.LessThan)
                {
                    return numDays < value;
                }
                else if (Operator == CriteriaOperator.LessThanOrEqual)
                {
                    return numDays <= value;
                }

                throw new Exception("INVALID DATE OPERATION");
            }

            object val = valueFromString(ValueDataType, givenValue);

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
                case CriteriaOperator.Populated:
                    return isPopulated(val);
                case CriteriaOperator.NotPopulated:
                    return isNotPopulated(val);
            }

            throw new Exception("INVALID COMPARISON OPERATION");
        }

        private bool isLessThan(object val)
        {
            switch (ValueDataType)
            {
                case CriteriaValueDataType.Integer:
                    return (int?)val < (int?)Value;
                case CriteriaValueDataType.Double:
                    return (double?)val < (double?)Value;
            }

            throw new Exception("ERROR - INVALID OPERATION FOR SPECIFIED DATA TYPE.");
        }

        private bool isGreaterThan(object val)
        {
            switch (ValueDataType)
            {
                case CriteriaValueDataType.Integer:
                    return (int?)val > (int?)Value;
                case CriteriaValueDataType.Double:
                    return (double?)val > (double?)Value;
            }

            throw new Exception("ERROR - INVALID OPERATION FOR SPECIFIED DATA TYPE.");
        }

        private bool isLessThanOrEqual(object val)
        {
            switch (ValueDataType)
            {
                case CriteriaValueDataType.Integer:
                    return (int?)val <= (int?)Value;
                case CriteriaValueDataType.Double:
                    return (double?)val <= (double?)Value;
            }

            throw new Exception("ERROR - INVALID OPERATION FOR SPECIFIED DATA TYPE.");
        }

        private bool isGreaterThanOrEqual(object val)
        {
            switch (ValueDataType)
            {
                case CriteriaValueDataType.Integer:
                    return (int?)val >= (int?)Value;
                case CriteriaValueDataType.Double:
                    return (double?)val >= (double?)Value;
            }

            throw new Exception("ERROR - INVALID OPERATION FOR SPECIFIED DATA TYPE.");
        }

        private bool isEqual(object val)
        {
            switch (ValueDataType)
            {
                case CriteriaValueDataType.Boolean:
                    return (bool?)val == (bool?)Value;
                case CriteriaValueDataType.String:
                    return string.Compare((string)val, (string)Value, true) == 0;
                case CriteriaValueDataType.Integer:
                    return (int?)val == (int?)Value;
                case CriteriaValueDataType.Double:
                    return (double?)val == (double?)Value;
                case CriteriaValueDataType.Lookup:
                    return (int?)val == (int?)Value;
            }

            throw new Exception("ERROR - INVALID OPERATION FOR SPECIFIED DATA TYPE.");
        }

        private bool isNotEqual(object val)
        {
            switch (ValueDataType)
            {
                case CriteriaValueDataType.Boolean:
                    return (bool?)val != (bool?)Value;
                case CriteriaValueDataType.String:
                    return string.Compare((string)val, (string)Value, true) != 0;
                case CriteriaValueDataType.Integer:
                    return (int?)val != (int?)Value;
                case CriteriaValueDataType.Double:
                    return (double?)val != (double?)Value;
                case CriteriaValueDataType.Lookup:
                    return (int?)val != (int?)Value;
            }

            throw new Exception("ERROR - INVALID OPERATION FOR SPECIFIED DATA TYPE.");
        }

        private bool isPopulated(object val)
        {
            switch (ValueDataType)
            {
                case CriteriaValueDataType.Boolean:
                case CriteriaValueDataType.Integer:
                case CriteriaValueDataType.Double:
                    return val != null;
                case CriteriaValueDataType.Lookup:
                    return val != null && (int)val > 0;
                case CriteriaValueDataType.String:
                    return !string.IsNullOrEmpty((string)val);
            }

            throw new Exception("ERROR - INVALID OPERATION FOR SPECIFIED DATA TYPE.");
        }

        private bool isNotPopulated(object val)
        {
            switch (ValueDataType)
            {
                case CriteriaValueDataType.Boolean:
                case CriteriaValueDataType.Integer:
                case CriteriaValueDataType.Double:
                    return val == null;
                case CriteriaValueDataType.Lookup:
                    return val == null || (int)val < 1;
                case CriteriaValueDataType.String:
                    return string.IsNullOrEmpty((string)val);
            }

            throw new Exception("ERROR - INVALID OPERATION FOR SPECIFIED DATA TYPE.");
        }
    }
}
