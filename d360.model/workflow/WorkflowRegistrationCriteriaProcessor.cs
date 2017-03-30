using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace d360.model.workflow
{
    public static class WorkflowRegistrationCriteriaProcessor
    {
        internal static List<WorkflowCriteriaExpressionModel> expression;

        public static bool Evaluate(CompanyContext context, string @object, int objectId, string criteria)
        {
            if (string.IsNullOrEmpty(criteria)) return true; // null criteria means all objects are applicable

            if (string.IsNullOrEmpty(@object) || objectId <= 0) throw new Exception("ERROR - A VALID OBJECT AND OBJECT ID MUST BE SPECIFIED.  THE OBJECT ID MUST BE GREATER THAN 0.");

            //take the string criteria and generate the class
            PopulateExpressionFromXml(criteria);

            //load the values for each of the fields for the given object
            return EvaluateObject(context, @object, objectId);            
        }

        public static string ToPlainText(CompanyContext context, string criteria)
        {
            if (string.IsNullOrEmpty(criteria)) return "";

            //take the string criteria and generate the class
            PopulateExpressionFromXml(criteria);

            StringBuilder sb = new StringBuilder();

            foreach (var item in expression)
            {
                if (sb.Length != 0) sb.Append(" AND ");

                sb.Append(item.ToPlainText(context));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Given an object determin if it matches this criteria
        /// </summary>
        /// <param name="context"></param>
        /// <param name="object"></param>
        /// <param name="objectId"></param>
        private static bool EvaluateObject(CompanyContext context, string @object, int objectId)
        {
            var fields = context.Fields.Where(x => x.ObjectID == objectId && x.ObjectType == @object);

            // no fields with an expression means no match
            if (!fields.Any() && expression.Count > 0) return false;

            foreach (var item in expression)
            {
                var value = fields.Where(x => x.FieldTypeID == item.FieldTypeId).FirstOrDefault();

                if (value == null) return false;

                if (!item.IsValueMatch(value.FormattedValue)) return false;
            }

            return true;
        }

        private static void PopulateExpressionFromXml(string criteria)
        {
            // PARSE THE XML
            XElement exprXml = null;
            try
            {
                exprXml = XElement.Parse(criteria);
            }
            catch
            {
                throw new Exception("ERROR - UNABLE TO PARSE CRITERIA XML USING XML PARSER");
            }

            // LOOP THROUGH EACH EXPRESSION
            expression = new List<WorkflowCriteriaExpressionModel>();

            foreach (var expr in exprXml.Elements("Condition"))
            {
                expression.Add(WorkflowCriteriaExpressionModel.Parse(expr));                
            }
        }
    }
}
