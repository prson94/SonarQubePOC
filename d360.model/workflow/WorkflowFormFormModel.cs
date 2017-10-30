using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace d360.model.workflow
{
    public class WorkflowFormFormModel
    {
        public List<WorkflowFormFieldModel> Fields { get; set; }

        internal static WorkflowFormFormModel ParseXml(XElement form)
        {
            var model = new WorkflowFormFormModel();
            model.Fields = new List<WorkflowFormFieldModel>();

            var fields = form.Elements("field");
            
            foreach (var field in fields)
            {
                model.Fields.Add(
                        new WorkflowFormFieldModel
                        {
                            FieldType = (string)field.Attribute("fieldtype"),
                            Label = (string)field.Attribute("label"),
                            Value = (string)field.Attribute("value"),
                            ID = ((string)field.Attribute("id")),
                            IntersectTypeID = (string)field.Attribute("intersectTypeId")
                        }
                    );
            }

            return model;
        }
    }
}
