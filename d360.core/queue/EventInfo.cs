using d360.core.enums.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.queue
{
    public class EventInfo
    {
        public string DomainPrefix { get; set; }

        public int CompanyID { get; set; }

        public int ResourceID { get; set; }

        public SystemObjects Object { get; set; }

        public int ObjectID { get; set; }

        public SystemObjects ObjectType { get; set; }

        public int ObjectTypeID { get; set; }

        public ChangeType Action { get; set; }
    }
}
