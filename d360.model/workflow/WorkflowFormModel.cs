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
                            ID = int.Parse(((string)field.Attribute("id"))??"0")
                        }
                    );
            }

            return model;
        }

        public string GetFormValueById(int id)
        {
            var res = Fields.Where(x => x.ID == id).FirstOrDefault();

            if (res == null) return "";

            return (res.Value??"").Trim();
        }
    }
}
