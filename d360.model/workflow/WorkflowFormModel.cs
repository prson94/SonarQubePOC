using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace d360.model.workflow
{
    public class WorkflowFormModel
    {
        public List<WorkflowFormFieldModel> Fields { get; set; }

        public static WorkflowFormModel ParseXml(XElement xml)
        {
            if (xml == null) throw new Exception("INVALID XML SPECIFIED FOR FORM MODEL");

            var model = new WorkflowFormModel();
            model.Fields = new List<WorkflowFormFieldModel>();

            var form = xml.Elements("form");

            if (form == null) return model;

            var fields = form.Elements("field");

            foreach (var field in fields)
            {
                model.Fields.Add(
                        new WorkflowFormFieldModel
                        {
                            FieldType = (string)field.Attribute("fieldtype"),
                            Label = (string)field.Attribute("label"),
                            Value = (string)field.Attribute("value"),
                            ID = ((string)field.Attribute("id"))
                        }
                    );
            }

            return model;
        }

        public string GetFormValueById(string id)
        {
            var res = Fields.Where(x => x.ID == id).FirstOrDefault();

            if (res == null) return "";

            return (res.Value??"").Trim();
        }
    }
}
