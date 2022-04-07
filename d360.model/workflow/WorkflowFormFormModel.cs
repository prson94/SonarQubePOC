using System.Collections.Generic;
using System.Xml.Linq;

namespace d360.model.workflow
{
    public class WorkflowFormFormModel
    {
        public List<WorkflowFormFieldModel> Fields { get; set; }

        internal static WorkflowFormFormModel ParseXml(XElement form)
        {
            WorkflowFormFormModel model = new WorkflowFormFormModel
            {
                Fields = new List<WorkflowFormFieldModel>()
            };

            IEnumerable<XElement> fields = form.Elements("field");

            foreach (XElement field in fields)
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
