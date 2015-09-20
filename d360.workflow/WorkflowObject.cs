using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.workflow
{
    public class WorkflowObject
    {
        public WorkflowObject()
        {
            Arguments = new Dictionary<string, object>();
        }

        public WorkflowAction To { get; set; }

        public int CompanyID { get; set; }

        public Dictionary<string, object> Arguments { get; set; }
    }
}
