using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.workflow.models
{
    public class RequestApproval
    {
        public int ResourceID { get; set; }
        public bool Approved { get; set; }
        public string Note { get; set; }
    }
}
