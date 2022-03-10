using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Views
{
    [DataContract(Namespace = NAMESPACE)]
    public class FollowDetail : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int ResourceID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int ObjectID { get; set; }

        [DataMember, Key, Column(Order = 3, TypeName = "varchar"), StringLength(50)]
        public string ObjectType { get; set; }

        [DataMember]
        public long? AssetID { get; set; }

        [DataMember]
        public int FollowID { get; set; }

        [DataMember]
        public int? ID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string TextPath { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public int? ParentID { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(19)]
        public string ParentType { get; set; }

        [DataMember]
        public string Url { get; set; }

        [DataMember]
        public int? TypeID { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(25)]
        public string Type { get; set; }

        [DataMember]
        public string TypeName { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(7)]
        public string IconBackColor { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(7)]
        public string IconForeColor { get; set; }

        [DataMember]
        public string IconText { get; set; }

        [DataMember]
        public int OpenEventCount { get; set; }

        [DataMember]
        public decimal? CurrentScore { get; set; }

        [DataMember]
        public string FollowerEmail { get; set; }

        [DataMember]
        public string FollowerFirstName { get; set; }

        [DataMember]
        public string FollowerLastName { get; set; }

        [DataMember]
        public string FollowerName { get; set; }

        [DataMember]
        public string FollowerObjectType { get; set; }

        [DataMember]
        public int FollowerObjectID { get; set; }

        [DataMember]
        public string FollowerUrl { get; set; }

        [DataMember]
        public bool HardFollow { get; set; }
    }
}
