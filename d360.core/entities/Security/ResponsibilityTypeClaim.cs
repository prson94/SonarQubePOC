using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(d360.core.ObjectTypeInfo.ResponsibilityTypeClaim, "ResponsibilityTypeClaim")]
    public class ResponsibilityTypeClaim : BaseIntObject, IIntObject
    {
        #region Properties

        [DataMember]
        public int ResponsibilityTypeID { get; set; }

        [DataMember]
        public Claim Claim { get; set; }

        [DataMember]
        public ClaimObject ClaimObject { get; set; }

        #endregion

        [IgnoreDataMember]
        public virtual ResponsibilityType ResponsibilityType { get; set; }
    }
}
