using d360.core.enums;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ResponsibilityTypeObjectClaimDetail: BaseObject
    {     
        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public int ResponsibilityTypeID { get; set; }

        [DataMember]
        public string ResponsibilityType { get; set; }

        [DataMember]
        public Claim Claim { get; set; }

        [DataMember, NotMapped]
        public string ClaimDescription 
        { 
            get  { return Enum.GetName(typeof(Claim), Claim); } 
        }

        [DataMember]
        public ClaimObject ClaimObject { get; set; }

        [DataMember, NotMapped]
        public string ClaimObjectDescription { get { return Enum.GetName(typeof(ClaimObject), ClaimObject); } }

        [DataMember]
        public string ObjectType { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public string ObjectName { get; set; }

        [DataMember]
        public string ObjectTypeName { get; set; }
    }
}
