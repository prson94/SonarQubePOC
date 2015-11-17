using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Follow : BaseObject
    {
        [Column(Order = 1), Key]
        public int ID { get; set; }

        [DataMember]
        public int ResourceID { get; set; }

        [DataMember]
        public string ObjectType { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public DateTime DateCreated { get; set; }

        [DataMember]
        public FollowType FollowTypeID { get; set; }

    }

    public class FollowChild : BaseObject
    {
        [Column(Order = 1), Key]
        public string ParentObjectType { get; set; }
        [Column(Order = 2), Key]
        public int ParentObjectID { get; set; }
        [DataMember]
        public int ObjectID { get; set; }
        [DataMember]
        public string ObjectType { get; set; }
        [DataMember]
        public DateTime DateCreated { get; set; }
        [DataMember]
        public FollowType FollowTypeID { get; set; }
    }

}
