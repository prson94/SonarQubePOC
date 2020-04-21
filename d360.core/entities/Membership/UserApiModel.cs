using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Membership
{
    public class UserApiDeleteModel
    {
        public Guid Uid { get; set; }
        public CompanyResource CompanyResource { get; set; }
        public GlobalReportingResource Resource { get; set; }
    }
}
