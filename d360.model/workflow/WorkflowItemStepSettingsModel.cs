using d360.core.enums.Workflow;
using System;
using System.Xml.Linq;

namespace d360.model.workflow
{
    public class WorkflowItemStepSettingModel
    {
        private static string RESPONSIBILITY_TYPE_ID = "ResponsibilityTypeID";
        private static string FORM_RESPONSE_TYPE = "FormResponseType";
        private static string WAIT_FOR_ALL = "WaitForAllTransitions";

        public int ResponsibilityTypeID { get; set; }
        public FormResponseType ResponseType { get; set; }

        public bool WaitForAllTransitions { get; set; }

        public static WorkflowItemStepSettingModel ParseXml(string root)
        {
            XElement xml = null;
            if(!string.IsNullOrEmpty(root))
            {
                xml = XElement.Parse(root);
            }

            return ParseXml(xml);
        }

        public static WorkflowItemStepSettingModel ParseXml(XElement root)
        {
            int responsibilityTypeID = -1;
            FormResponseType responseType = FormResponseType.FirstResponse;
            bool waitForAll = false;

            if (root != null)
            {
                if(root.Element(RESPONSIBILITY_TYPE_ID) != null)
                {
                    int.TryParse(root.Element(RESPONSIBILITY_TYPE_ID).Value, out responsibilityTypeID);
                }

                if (root.Element(FORM_RESPONSE_TYPE) != null)
                {
                    responseType = (FormResponseType)Enum.Parse(typeof(FormResponseType), root.Element(FORM_RESPONSE_TYPE).Value);                    
                }

                if(root.Element(WAIT_FOR_ALL) != null)
                {
                    bool.TryParse(root.Element(WAIT_FOR_ALL).Value, out waitForAll);
                }
            }

            return new WorkflowItemStepSettingModel
            {
                ResponseType = responseType,
                ResponsibilityTypeID = responsibilityTypeID,
                WaitForAllTransitions = waitForAll
            };
        }
    }
}
