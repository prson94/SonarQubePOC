using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities.Views
{
    [DataContract(Namespace = NAMESPACE)]
    public class FollowDetail: BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int ResourceID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int ObjectID { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public string ObjectType { get; set; }

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

        [DataMember]
        public string ParentType { get; set; }

        [DataMember]
        public string Url { get; set; }

        [DataMember]
        public int? TypeID { get; set; }

        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public string TypeName { get; set; }

        [DataMember]
        public string IconBackColor { get; set; }

        [DataMember]
        public string IconForeColor { get; set; }

        [DataMember]
        public string IconText { get; set; }

        [DataMember]
        public int OpenEventCount { get; set; }

        [DataMember]
        public double? CurrentScore { get; set; }


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
