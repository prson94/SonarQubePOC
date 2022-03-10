using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class IntersectDetail : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int ID { get; set; }

        [DataMember]
        public int IntersectTypeID { get; set; }

        [DataMember]
        public Guid IntersectTypeUid { get; set; }

        [DataMember, Key, Column(Order = 2, TypeName = "varchar"), StringLength(50)]
        public string Subject { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public int SubjectID { get; set; }

        [DataMember]
        public string SubjectName { get; set; }

        [DataMember]
        public string SubjectUrl { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string SubjectType { get; set; }

        [DataMember]
        public int SubjectTypeID { get; set; }

        [DataMember]
        public string SubjectTypeName { get; set; }

        [DataMember]
        public string SubjectIconBackColor { get; set; }

        [DataMember]
        public string SubjectIconForeColor { get; set; }

        [DataMember]
        public string SubjectIconText { get; set; }

        [DataMember, Key, Column(Order = 4, TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember, Key, Column(Order = 5)]
        public int ObjectID { get; set; }

        [DataMember]
        public string ObjectName { get; set; }

        [DataMember]
        public string ObjectUrl { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string ObjectType { get; set; }

        [DataMember]
        public int ObjectTypeID { get; set; }

        [DataMember]
        public string ObjectTypeName { get; set; }

        [DataMember]
        public string ObjectIconBackColor { get; set; }

        [DataMember]
        public string ObjectIconForeColor { get; set; }

        [DataMember]
        public string ObjectIconText { get; set; }

        [DataMember]
        public Guid PredicateUid { get; set; }

        [DataMember]
        public int? PredicateID { get; set; }

        [DataMember]
        public string PredicateName { get; set; }

        [DataMember]
        public PredicateType? PredicateType { get; set; }

        [DataMember]
        public Guid? ObjectUid { get; set; }

        [DataMember]
        public Guid? SubjectUid { get; set; }
    }
}
