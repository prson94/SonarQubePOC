using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Data.Entity;

namespace d360.model.workflow
{
    public static class WorkflowRegistrationCriteriaProcessor
    {
        public static bool Evaluate(CompanyContext context, string @object, int objectId, string criteria, long itemId = -1, int score = -1, List<int> changedFields = null, string issueObject = "", int issueObjectId = -1)
        {
            if (string.IsNullOrEmpty(criteria)) return true; // null criteria means all objects are applicable

            if (string.IsNullOrEmpty(@object) || objectId <= 0) throw new Exception("ERROR - A VALID OBJECT AND OBJECT ID MUST BE SPECIFIED.  THE OBJECT ID MUST BE GREATER THAN 0.");

            //take the string criteria and generate the class
            List<WorkflowCriteriaExpressionModel> expression = PopulateExpressionFromXml(criteria);
            bool satisfyAll = expression.All(x => x.CriteriaConnector == core.enums.Workflow.CriteriaConnector.AND); 

            //load the values for each of the fields for the given object
            return EvaluateObject(expression, context, @object, objectId, itemId, score, issueObject, issueObjectId, changedFields, satisfyAll);
        }

        public static string ToPlainText(ICompanyContext context, string criteria)
        {
            if (string.IsNullOrEmpty(criteria)) return "";

            //take the string criteria and generate the class
            List<WorkflowCriteriaExpressionModel> expression = PopulateExpressionFromXml(criteria);

            StringBuilder sb = new StringBuilder();

            foreach (var item in expression)
            {
                if (sb.Length != 0) sb.Append(" AND ");

                sb.Append(item.ToPlainText(context));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Given an object determine if it matches this criteria
        /// </summary>
        /// <param name="context"></param>
        /// <param name="object"></param>
        /// <param name="objectId"></param>
        private static bool EvaluateObject(List<WorkflowCriteriaExpressionModel> expression, CompanyContext context, string @object, int objectId, long itemId, int score = -1, string issueObjectType = "", int issueObjectTypeId = -1, List<int> changedFields = null, bool satisfyAll = true)
        {
            var fields = context.Fields.Where(x => x.ObjectID == objectId && x.ObjectType == @object);

            foreach (var item in expression)
            {
                item.IsCriteriaChecked = EvaluateField(context, item, fields, @object, objectId, itemId, score, issueObjectType, issueObjectTypeId, changedFields);
                if (satisfyAll && item.IsCriteriaChecked == false) return false;
                if (!satisfyAll && item.IsCriteriaChecked == true) return true;
            }

            return satisfyAll ? expression.All(x => x.IsCriteriaChecked) : expression.Any(x => x.IsCriteriaChecked);
        }

        private static bool EvaluateField(ICompanyContext context, WorkflowCriteriaExpressionModel item, IQueryable<Field> fields, string @object, int objectId, long itemId, int score = -1, string issueObjectType = "", int issueObjectTypeId = -1, List<int> changedFields = null)
        {
            if (item.FieldTypeId > 0)
            {
                var value = fields.Where(x => x.FieldTypeID == item.FieldTypeId).FirstOrDefault();

                if (value == null) return false;

                //special case for changed operator. If it's in the list of changed fields, return true
                if (item.Operator == core.enums.Workflow.CriteriaOperator.Changed)
                    return changedFields.Contains(item.FieldTypeId);

                if (item.ValueDataType == core.enums.Workflow.CriteriaValueDataType.Lookup)
                {
                    if (!item.IsValueMatch(value.Value)) return false;
                }
                else
                {
                    if (!item.IsValueMatch(value.FormattedValue)) return false;
                }
            }
            else if ((item.ContextualFieldID ?? "").ToLower() == "score")
            {
                if (!item.IsValueMatch(score.ToString())) return false;
            }
            else if ((item.ContextualFieldID ?? "").ToLower() == "requestedon")
            {
                var requestedOn = context.GetById<ShoppingCart>(objectId).RequestedOn;
                if (!item.IsValueMatch(requestedOn.ToString())) return false;
            }
            else if ((item.ContextualFieldID ?? "").ToLower() == "name")
            {
                var name = context.GetObjectDetail(@object, objectId).Name;
                if (!item.IsValueMatch(name?.ToString() ?? "")) return false;
            }
            else if ((item.ContextualFieldID ?? "").ToLower() == "description")
            {
                var description = context.GetObjectDetail(@object, objectId).Description;
                if (!item.IsValueMatch(description?.ToString() ?? "")) return false;
            }
            else if ((item.ContextualFieldID ?? "").ToLower() == "issueobject")
            {
                if (!item.IsValueMatch(issueObjectType)) return false;
            }
            else if ((item.ContextualFieldID ?? "").ToLower() == "issueobjectid")
            {
                if (!item.IsValueMatch(issueObjectTypeId.ToString())) return false;
            }
            else if (item.VersionStepId > 0)
            {
                //load the results of the form version step
                var formStep = context.WorkflowItemSteps.Where(x => x.ItemID == itemId && x.StepID == item.VersionStepId).Include(x => x.Step).OrderByDescending(x => x.ID).FirstOrDefault();

                if (formStep == null)
                {
                    Console.WriteLine("DEBUG - CANNOT FIND THE RESULTS OF THE FORM STEP SELECTED.");

                    return false;
                }

                //get the results of the form input with the specified id
                var xml = formStep.Fields;

                if (string.IsNullOrEmpty(xml))
                {
                    Console.WriteLine("DEBUG - FORM RESULT HAS NO XML");

                    return false;
                }

                var formModel = WorkflowFormModel.ParseXml(XElement.Parse(xml));

                if ((formStep.Step == null) || string.IsNullOrEmpty(formStep.Step.Settings))
                {
                    Console.WriteLine("DEBUG - FORM SETTINGS ARE MISSING");

                    return false;
                }

                //check the form response type is it all, first or majority
                var formSettings = WorkflowItemStepSettingModel.ParseXml(XElement.Parse(formStep.Step.Settings));

                switch (formSettings.ResponseType)
                {
                    case core.enums.Workflow.FormResponseType.FirstResponse:
                        {
                            //check if the value matches
                            var formValue = formModel.GetFormValueById(item.FormInputId);

                            if (item.Operator == core.enums.Workflow.CriteriaOperator.NotEqual)
                            {
                                //
                                if (string.Compare(formValue, (item.Value.ToString() ?? "").Trim(), true) == 0) return false;
                            }
                            else
                            {
                                //true operator
                                if (string.Compare(formValue, (item.Value.ToString() ?? "").Trim(), true) != 0) return false;

                            }
                            break;
                        }
                    case core.enums.Workflow.FormResponseType.All:
                        {
                            // ALL USERS NEED TO RESPOND AND APPROVE

                            // GET RESPONSES FROM EACH FORM AND MAKE SURE THEY ARE THE SAME IF NOT RETURN FALSE
                            var formValues = formModel.GetFormValuesById(item.FormInputId);

                            foreach (var val in formValues)
                            {
                                if (item.Operator == core.enums.Workflow.CriteriaOperator.NotEqual)
                                {
                                    if (string.Compare(val, (item.Value.ToString() ?? "").Trim(), true) == 0) return false;
                                }
                                else
                                {
                                    if (string.Compare(val, (item.Value.ToString() ?? "").Trim(), true) != 0) return false;
                                }

                            }
                            return true;
                        }
                    case core.enums.Workflow.FormResponseType.Majority:
                        {
                            var formValues = formModel.GetFormValuesById(item.FormInputId);

                            var matchCount = 0;

                            foreach (var val in formValues)
                            {
                                if (item.Operator == core.enums.Workflow.CriteriaOperator.NotEqual)
                                {
                                    if (string.Compare(val, (item.Value.ToString() ?? "").Trim(), true) != 0) matchCount++;
                                }
                                else
                                {
                                    if (string.Compare(val, (item.Value.ToString() ?? "").Trim(), true) == 0) matchCount++;
                                }

                            }

                            return matchCount > (formValues.Count / 2);
                        }
                    default:
                        Console.WriteLine("DEBUG - FORM HAS UNKNOWN OR UNSUPPORTED FORM RESPONSE TYPE");

                        return false;
                }
            }

            return true;
        }

        private static List<WorkflowCriteriaExpressionModel> PopulateExpressionFromXml(string criteria)
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
            List<WorkflowCriteriaExpressionModel> expression = new List<WorkflowCriteriaExpressionModel>();

            foreach (var expr in exprXml.Elements("Condition"))
            {
                expression.Add(WorkflowCriteriaExpressionModel.Parse(expr));
            }

            return expression;
        }
    }
}
