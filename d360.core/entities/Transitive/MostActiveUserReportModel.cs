using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    public class MostActiveUserReportModel
    {
        public int ResourceID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int ActivityCount { get; set; }
    }
}
