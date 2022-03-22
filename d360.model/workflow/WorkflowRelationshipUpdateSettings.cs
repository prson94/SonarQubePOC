using System.Xml.Linq;

namespace d360.model.workflow
{
    public class WorkflowRelationshipUpdateSettings
    {
        private static readonly string CLEAR_VALUE = "ClearValue";
        private static readonly string APPEND_VALUE = "AppendValue";
        private static readonly string FORM_FIELD_ID = "FormFieldId";
        private static readonly string FORM_STEP_ID = "FormStepId";

        public bool ClearValue { get; set; }

        public bool AppendValue { get; set; }

        public string FormField { get; set; }

        public int FormStepID { get; set; }

        public static WorkflowRelationshipUpdateSettings ParseXml(XElement xml)
        {
            WorkflowRelationshipUpdateSettings model = new WorkflowRelationshipUpdateSettings();

            bool isClearValue = false;
            bool isAppendValue = false;
            int formStepId = 0;
            string formField = "";

            if (xml.Attribute(CLEAR_VALUE) != null)
            {
                bool.TryParse(xml.Attribute(CLEAR_VALUE).Value, out isClearValue);
            }

            if (xml.Attribute(FORM_FIELD_ID) != null)
            {
                formField = xml.Attribute(FORM_FIELD_ID).Value;
            }

            if (xml.Attribute(FORM_STEP_ID) != null)
            {
                int.TryParse(xml.Attribute(FORM_STEP_ID).Value, out formStepId);
            }

            if (xml.Attribute(APPEND_VALUE) != null)
            {
                bool.TryParse(xml.Attribute(APPEND_VALUE).Value, out isAppendValue);
            }

            model.ClearValue = isClearValue;
            model.FormField = formField;
            model.FormStepID = formStepId;
            model.AppendValue = isAppendValue;

            return model;
        }
    }
}
