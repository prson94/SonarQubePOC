using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;

namespace d360.core.entities
{
    /// <summary>
    /// Defines what types of artifacts can be assigned as a source for a given responsibility type.
    /// </summary>
    [DataContract(Namespace = NAMESPACE)]
    public class ResponsibilityTypeRelation : BaseObject, IUpdatedMetadata, ICreatedMetadata
    {
        [Key, Column(Order = 1), DataMember]
        public int ResponsibilityTypeID { get; set; }

        [Key, Column(Order = 2, TypeName = "varchar"), StringLength(50), DataMember]
        public string ObjectType { get; set; }

        [Key, Column(Order = 3), DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public int PermissionsBitMask { get; set; }

        public virtual ResponsibilityType ResponsibilityType { get; set; }

        public DateTime? CreatedOn { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public int? UpdatedBy { get; set; }
    }
}
