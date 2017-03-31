using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.workflow
{
    public class WorkflowFormFieldModel
    {
        public int ID { get; set; }
        public string Label { get; set; }
        public string Value { get; set; }
        public string FieldType { get; set; }
    }
}
