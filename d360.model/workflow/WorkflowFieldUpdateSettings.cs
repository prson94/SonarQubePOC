using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace d360.model.workflow
{
    public class WorkflowFieldUpdateSettings
    {
        private static string FIELD_ID = "FieldId";
        private static string VALUE = "Value";
        private static string CURRENT_DATE = "UseCurrentDate";

        public int FieldID { get; set; }

        public string Value { get; set; }

        public bool CurrentDate { get; set; }


        public static WorkflowFieldUpdateSettings ParseXml(XElement xml)
        {
            var model = new WorkflowFieldUpdateSettings();

            int fieldId = 0;
            string value = "";
            bool isCurrentDate = false;

            if (xml.Attribute(FIELD_ID) != null)
            {
                int.TryParse(xml.Attribute(FIELD_ID).Value, out fieldId);                
            }

            if(xml.Attribute(VALUE) != null)
            {
                value = xml.Attribute(VALUE).Value;                
            }

            if (xml.Attribute(CURRENT_DATE) != null)
            {                
                bool.TryParse(xml.Attribute(CURRENT_DATE).Value, out isCurrentDate);
            }

            model.CurrentDate = isCurrentDate;
            model.FieldID = fieldId;
            model.Value = value;

            return model;
        }
    }
}
