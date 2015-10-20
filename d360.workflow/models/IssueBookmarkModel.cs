using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.workflow.models
{
    public class IssueBookmarkModel
    {
        public int ResourceID { get; set; }
        public string Action { get; set; }
        public string Comment { get; set; }
        public string ReAssignToResourceObject { get; set; }
        public int? ReAssignToResourceObjectID { get; set; }
    }
}
