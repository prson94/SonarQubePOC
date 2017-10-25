using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace d360.model.workflow
{
    public class WorkflowRelationshipUpdateSettings
    {
        private static string INTERSECTTYPE_ID = "IntersectTypeID";
        
        private static string CLEAR_VALUE = "ClearValue";
        private static string APPEND_VALUE = "AppendValue";

        private static string FORM_FIELD_ID = "FormFieldId";
        private static string FORM_STEP_ID = "FormStepId";
        

        public int IntersectTypeID { get; set; }
        
        public bool ClearValue { get; set; }

        public bool AppendValue { get; set; }

        
        public string FormField { get; set; }
        public int FormStepID { get; set; }


        public static WorkflowRelationshipUpdateSettings ParseXml(XElement xml)
        {
            var model = new WorkflowRelationshipUpdateSettings();

            int intersectTypeID = 0;                        
            bool isClearValue = false;            
            bool isAppendValue = false;
            int formStepId = 0;
            string formField = "";

            if (xml.Attribute(INTERSECTTYPE_ID) != null)
            {
                int.TryParse(xml.Attribute(INTERSECTTYPE_ID).Value, out intersectTypeID);
            }

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
                        
            model.IntersectTypeID = intersectTypeID;            
            model.ClearValue = isClearValue;            
            model.FormField = formField;
            model.FormStepID = formStepId;
            model.AppendValue = isAppendValue;

            return model;
        }
    }
}

