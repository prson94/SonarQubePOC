using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using d360.core.enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ResponsibilityTypeObjectClaim : BaseIntObject, IIntObject
    {
        #region Properties

        [DataMember]
        public int ResponsibilityTypeID { get; set; }

        [DataMember]
        public Claim Claim { get; set; }

        [DataMember]
        public ClaimObject ClaimObject { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string ObjectType { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        #endregion

        [IgnoreDataMember]
        public virtual ResponsibilityType ResponsibilityType { get; set; }
    }
}
