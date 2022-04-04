using System.Xml.Linq;

namespace d360.model.workflow
{
    public class WorkflowFieldUpdateSettings
    {
        private static readonly string FIELD_ID = "FieldId";
        private static readonly string VALUE = "Value";
        private static readonly string CURRENT_DATE = "UseCurrentDate";
        private static readonly string CLEAR_VALUE = "ClearValue";
        private static readonly string USEFORM_VALUE = "UseFormValue";
        private static readonly string FORM_FIELD_ID = "FormFieldId";
        private static readonly string FORM_STEP_ID = "FormStepId";
        private static readonly string APPEND_VALUE = "AppendValue";
        private static readonly string OBJECT_TYPE = "ObjectType";
        private static readonly string IS_ACTION_FORM = "IsActionForm";
        private static readonly string USE_OUTPUT_VALUE = "UseOutputValue";

        public int FieldID { get; set; }

        public string Value { get; set; }

        public string ObjectType { get; set; }
        
        public bool CurrentDate { get; set; }
        
        public bool ClearValue { get; set; }

        public bool AppendValue { get; set; }

        public bool UseFormValue { get; set; }
        
        public bool IsActionForm { get; set; }
        
        public bool UseOutputValue { get; set; }
        
        public string FormField { get; set; }
        
        public int FormStepID { get; set; }

        public static WorkflowFieldUpdateSettings ParseXml(XElement xml)
        {
            WorkflowFieldUpdateSettings model = new WorkflowFieldUpdateSettings();

            int fieldId = 0;
            string value = "";
            bool isCurrentDate = false;
            bool isClearValue = false;
            bool useFormValue = false;
            bool useOutputValue = false;
            bool isAppendValue = false;
            bool isActionForm = false;
            int formStepId = 0;
            string formField = "";
            string objectType = "";

            if (xml.Attribute(FIELD_ID) != null)
            {
                int.TryParse(xml.Attribute(FIELD_ID).Value, out fieldId);
            }

            if (xml.Attribute(VALUE) != null)
            {
                value = xml.Attribute(VALUE).Value;
            }

            if (xml.Attribute(OBJECT_TYPE) != null)
            {
                objectType = xml.Attribute(OBJECT_TYPE).Value;
            }

            if (xml.Attribute(CURRENT_DATE) != null)
            {
                bool.TryParse(xml.Attribute(CURRENT_DATE).Value, out isCurrentDate);
            }

            if (xml.Attribute(CLEAR_VALUE) != null)
            {
                bool.TryParse(xml.Attribute(CLEAR_VALUE).Value, out isClearValue);
            }

            if (xml.Attribute(USEFORM_VALUE) != null)
            {
                bool.TryParse(xml.Attribute(USEFORM_VALUE).Value, out useFormValue);
            }

            if (xml.Attribute(USE_OUTPUT_VALUE) != null)
            {
                bool.TryParse(xml.Attribute(USE_OUTPUT_VALUE).Value, out useOutputValue);
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

            if (xml.Attribute(IS_ACTION_FORM) != null)
            {
                bool.TryParse(xml.Attribute(IS_ACTION_FORM).Value, out isActionForm);
            }

            model.CurrentDate = isCurrentDate;
            model.FieldID = fieldId;
            model.Value = value;
            model.ClearValue = isClearValue;
            model.UseFormValue = useFormValue;
            model.UseOutputValue = useOutputValue;
            model.FormField = formField;
            model.FormStepID = formStepId;
            model.AppendValue = isAppendValue;
            model.ObjectType = objectType;
            model.IsActionForm = isActionForm;

            return model;
        }
    }
}
