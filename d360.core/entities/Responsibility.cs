using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(d360.core.ObjectTypeInfo.Responsibility, "Responsibility")]
    public class Responsibility : BaseIntObject, IIntObject, IUpdatedMetadata
    {
        #region Properties

        [DataMember]
        public int ResponsibilityTypeID { get; set; }

        [DataMember]
        public string ObjectType { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public string ResponsibleObjectType { get; set; }

        [DataMember]
        public int ResponsibleObjectID { get; set; }

        public bool Visible { get; set; }

        [DataMember]
        public int? TargetResponsibilityID { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        #endregion

        [IgnoreDataMember, ForeignKey("TargetResponsibilityID")]
        public virtual Responsibility TargetResponsibility { get; set; }

        [IgnoreDataMember]
        public virtual ResponsibilityType ResponsibilityType { get; set; }

        [IgnoreDataMember]
        public virtual ICollection<ResponsibilityContextItem> ResponsibilityContextItems { get; set; }

        [IgnoreDataMember]
        public virtual ICollection<ResponsibilityTransformation> ResponsibilityTransformations { get; set; }
    }
}
