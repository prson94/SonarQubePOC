using d360.core.enums.Workflow;
using System;
using System.Xml.Linq;

namespace d360.model.workflow
{
    public class WorkflowFormSettingsModel
    {
        private static string RESPONSIBILITY_TYPE_ID = "ResponsibilityTypeID";
        private static string FORM_RESPONSE_TYPE = "FormResponseType";

        public int ResponsibilityTypeID { get; set; }
        public FormResponseType ResponseType { get; set; }

        public static WorkflowFormSettingsModel ParseXml(XElement root)
        {
            int responsibilityTypeID = -1;
            FormResponseType responseType = FormResponseType.FirstResponse;

            if(root != null)
            {
                if(root.Element(RESPONSIBILITY_TYPE_ID) != null)
                {
                    int.TryParse(root.Element(RESPONSIBILITY_TYPE_ID).Value, out responsibilityTypeID);
                }

                if (root.Element(FORM_RESPONSE_TYPE) != null)
                {
                    responseType = (FormResponseType)Enum.Parse(typeof(FormResponseType), root.Element(FORM_RESPONSE_TYPE).Value);                    
                }
            }

            return new WorkflowFormSettingsModel
            {
                ResponseType = responseType,
                ResponsibilityTypeID = responsibilityTypeID
            };
        }
    }
}
