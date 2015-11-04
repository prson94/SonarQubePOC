using d360.core.enums;

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
