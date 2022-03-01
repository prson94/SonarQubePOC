using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Data.Entity;
using d360.core.enums.Workflow;
using d360.core.enums;

namespace d360.model.workflow
{
    public static class WorkflowRegistrationCriteriaProcessor
    {
        public static bool Evaluate(ICompanyContext context, string @object, int objectId, string criteria, long itemId = -1, List<int> changedFields = null, string issueObject = "", int issueObjectId = -1, int? scoreType = null)
        {
            if (string.IsNullOrEmpty(criteria)) return true; // null criteria means all objects are applicable

            if (string.IsNullOrEmpty(@object) || objectId <= 0) throw new Exception("ERROR - A VALID OBJECT AND OBJECT ID MUST BE SPECIFIED.  THE OBJECT ID MUST BE GREATER THAN 0.");

            //take the string criteria and generate the class
            List<WorkflowCriteriaExpressionModel> expression = PopulateExpressionFromXml(criteria);
            bool satisfyAll = expression.All(x => x.CriteriaConnector == core.enums.Workflow.CriteriaConnector.AND); 

            //load the values for each of the fields for the given object
            return EvaluateObject(expression, context, @object, objectId, itemId, issueObject, issueObjectId, changedFields, satisfyAll, scoreType);
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
        private static bool EvaluateObject(List<WorkflowCriteriaExpressionModel> expression, ICompanyContext context, string @object, int objectId, long itemId, string issueObjectType = "", int issueObjectTypeId = -1, List<int> changedFields = null, bool satisfyAll = true, int? scoreType = null)
        {

            //If there are no conditions object is eligible for workflow
            if (expression.Count == 0)
                return true;

            //since field and object events come in separately, we need to skip eval in some cases to prevent duplicate runs
            //1. There is a change condition on the workflow, and no change fields are present: Ignore the initial object event and wait for the field event to come in
            bool hasChangeCondition = expression.Any(e => e.Operator == core.enums.Workflow.CriteriaOperator.Changed);
            
            if (satisfyAll && hasChangeCondition && !changedFields.Any()) return false;

            var fields = context.Fields.Where(x => x.ObjectID == objectId && x.ObjectType == @object);
            
            if (issueObjectTypeId > -1)
            {
                //get asset fields for this action
                var issue = context.Issues.FirstOrDefault(x => x.ID == objectId);
                if (issue != null)
                {
                    fields = fields.Union(context.Fields.Where(x => x.ObjectID == issue.ObjectID && x.ObjectType == issue.Object));
                }
            }

            //system expressions are checked separately from user expressions and are always ANDed together
            List<string> systemContextualFields = new List<string>
            {
                "IssueObject",
                "IssueObjectID",
                "ScoreType"
            };

            var systemExpressions = expression.Where(x => systemContextualFields.Contains(x.ContextualFieldID));
            var userExpressions = expression.Where(x => !systemContextualFields.Contains(x.ContextualFieldID));

            foreach(var item in systemExpressions)
            {
                item.IsCriteriaChecked = EvaluateField(context, item, fields, @object, objectId, itemId, issueObjectType, issueObjectTypeId, changedFields, scoreType);
            }

            if (!systemExpressions.All(x => x.IsCriteriaChecked))
            {
                return false;
            }

            foreach (var item in userExpressions)
            {
                item.IsCriteriaChecked = EvaluateField(context, item, fields, @object, objectId, itemId, issueObjectType, issueObjectTypeId, changedFields, scoreType);
                if (satisfyAll && item.IsCriteriaChecked == false) return false;
                if (!satisfyAll && item.IsCriteriaChecked == true) return true;
            }

            return satisfyAll ? userExpressions.All(x => x.IsCriteriaChecked) : userExpressions.Any(x => x.IsCriteriaChecked);
        }

        private static bool EvaluateField(ICompanyContext context, WorkflowCriteriaExpressionModel item, IQueryable<Field> fields, string @object, int objectId, long itemId, string issueObjectType = "", int issueObjectTypeId = -1, List<int> changedFields = null, int? scoreType = null)
        {
            // If evaluated field is not part of changed fields return false
            // With this, we avoid triggering workflow again on plain save where field meets condition but is not changed
            if (changedFields != null && item.FieldTypeId > 0)
            {
                var field = fields.FirstOrDefault(x => x.FieldTypeID == item.FieldTypeId);
                var value = field?.Value ?? null;
                var formattedVal = field?.FormattedValue ?? null;

                //special case for changed operator. If it's in the list of changed fields, return true
                if (item.Operator == CriteriaOperator.Changed)
                    return changedFields.Contains(item.FieldTypeId);

                if (item.ValueDataType == CriteriaValueDataType.Lookup)
                {
                    if (!item.IsValueMatch(value)) return false;
                }
                else
                {
                    if (!item.IsValueMatch(formattedVal)) return false;
                }
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
            else if ((item.ContextualFieldID ?? "").ToLower() == "scoretype")
            {
                if (!item.IsValueMatch(scoreType?.ToString() ?? "")) return false;
            }
            else if ((item.ContextualFieldID ?? "").ToLower().StartsWith("score|"))
            {
                var typeString = item.ContextualFieldID.Split('|')[1];
                if (Enum.TryParse(typeString, out ScoreType stype))
                {
                    long? assetId = context.GetObjectDetail(@object, objectId)?.AssetID;
                    if (assetId.HasValue)
                    {
                        decimal? score = context.GetAssetScore((int)assetId, stype);
                        if (!item.IsValueMatch(score.ToString())) return false;
                    }
                    else
                    {
                        Console.WriteLine("DEBUG - CANNOT FIND ASSET FOR SCORE CONDITION");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("DEBUG - CANNOT FIND SCORE TYPE FOR SCORE CONDITION.");
                    return false;
                }
            }
            else if (item.VersionStepId > 0)
            {
                //load the results of the version step
                var versionStep = context.WorkflowItemSteps.Where(x => x.ItemID == itemId && x.StepID == item.VersionStepId).Include(x => x.Step).OrderByDescending(x => x.ID).FirstOrDefault();

                if (versionStep == null)
                {
                    Console.WriteLine("DEBUG - CANNOT FIND THE RESULTS OF THE FORM STEP SELECTED.");

                    return false;
                }

                //get the results of the form input with the specified id
                var xml = versionStep.Fields;

                if (string.IsNullOrEmpty(xml))
                {
                    Console.WriteLine("DEBUG - FORM RESULT HAS NO XML");

                    return false;
                }

                

                if ((versionStep.Step == null) || string.IsNullOrEmpty(versionStep.Step.Settings))
                {
                    Console.WriteLine("DEBUG - FORM SETTINGS ARE MISSING");

                    return false;
                }

                var stepSettings = WorkflowItemStepSettingModel.ParseXml(XElement.Parse(versionStep.Step.Settings));

                switch(versionStep.Step.ActivityType)
                {
                    case WorkflowActivityType.Form:
                    case WorkflowActivityType.None:
                        var formModel = WorkflowFormModel.ParseXml(XElement.Parse(xml));

                        switch (stepSettings.ResponseType)
                        {
                            case FormResponseType.FirstResponse:
                                {
                                    //check if the value matches
                                    var formValue = formModel.GetFormValueById(item.FormInputId);

                                    if (item.Operator == CriteriaOperator.NotEqual)
                                    {
                                        //
                                        if (string.Compare(formValue, (item.Value.ToString() ?? "").Trim(), true) == 0) return false;
                                    }
                                    else if (item.Operator == CriteriaOperator.Equal)
                                    {
                                        //true operator
                                        if (string.Compare(formValue, (item.Value.ToString() ?? "").Trim(), true) != 0) return false;

                                    }
                                    else if (item.Operator == CriteriaOperator.Populated)
                                    {
                                        return formValue.Length > 0;
                                    }
                                    else if (item.Operator == CriteriaOperator.NotPopulated)
                                    {
                                        return formValue.Length <= 0;
                                    }
                                    break;
                                }
                            case FormResponseType.All:
                                {
                                    // ALL USERS NEED TO RESPOND AND APPROVE

                                    // GET RESPONSES FROM EACH FORM AND MAKE SURE THEY ARE THE SAME IF NOT RETURN FALSE
                                    var formValues = formModel.GetFormValuesById(item.FormInputId);

                                    foreach (var val in formValues)
                                    {
                                        if (item.Operator == CriteriaOperator.NotEqual)
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
                            case FormResponseType.Majority:
                                {
                                    var formValues = formModel.GetFormValuesById(item.FormInputId);

                                    var matchCount = 0;

                                    foreach (var val in formValues)
                                    {
                                        if (item.Operator == CriteriaOperator.NotEqual)
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
                        break;
                    case WorkflowActivityType.HTTPRequest:
                        switch(item.FormInputId.ToUpper())
                        {
                            case "STATUSCODE":
                                var statusCodeString = versionStep.FieldsDocument?.Element("HTTPResponse")?.Element("StatusCode")?.Value ?? "";
                                if (int.TryParse(statusCodeString, out int _))
                                {
                                    return item.IsValueMatch(statusCodeString);
                                }
                                break;
                            case "RESPONSEBODY":
                                var responseBody = versionStep.FieldsDocument?.Element("HTTPResponse")?.Element("Body")?.Value ?? "";
                                return item.IsValueMatch(responseBody);

                        }
                        break;
                    case WorkflowActivityType.HTTPResponse:
                        var stepFields = versionStep.FieldsDocument;
                        if (stepFields != null)
                        {
                            var outputs = stepFields.Element("Outputs").Elements("Output");
                            if (outputs != null)
                            {
                                foreach (var output in outputs)
                                {
                                    if (output.Element("Id")?.Value == item.FormInputId)
                                    {
                                        return item.IsValueMatch(output.Element("Value")?.Value ?? "");
                                    }
                                }
                            }
                        }
                        break;
                    default:
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
