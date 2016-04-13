using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.enums;

namespace d360.core.entities.Views
{
    [DataContract(Namespace = NAMESPACE)]
    public class ResponsibilitySummaryDetail : BaseObject
    {
        #region Properties

        [DataMember, Key, Column(Order = 1)]
        public int ResponsibilityID { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string ObjectType { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public string ObjectName { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(500)]
        public string ObjectUrl { get; set; }

        [DataMember]
        public string ObjectTypeName { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int ResponsibilityTypeID { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string ResponsibleObjectType { get; set; }

        [DataMember]
        public int ResponsibleObjectID { get; set; }

        [DataMember]
        public string ResponsibleObjectName { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(500)]
        public string ResponsibleObjectUrl { get; set; }

        [DataMember]
        public string ResponsibleObjectTypeName { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(250)]
        public string ResponsibilityType { get; set; }

        [DataMember]
        public ResponsibilityTypeGroup ResponsibilityTypeGroup { get; set; }

        #endregion
    }
}
