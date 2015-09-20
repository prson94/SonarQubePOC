using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    public class ResponsibilityTypeClaimDetail
    {
        public Claim Claim { get; set; }
        public ClaimObject ClaimObject { get; set; }
        public int ID { get; set; }
        public string ResponsibilityType { get; set; }
        public int ResponsibilityTypeID { get; set; }
    }
}
