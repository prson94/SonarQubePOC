using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.enums;
using System;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class IntersectTypeDetail : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int ID { get; set; }

        [DataMember]
        public Guid Uid { get; set; }

        [DataMember, Key, Column(Order = 2, TypeName = "varchar"), StringLength(50)]
        public string Subject { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public int SubjectID { get; set; }

        [DataMember]
        public Cardinality SubjectCardinality { get; set; }

        [DataMember]
        public string SubjectName { get; set; }

        [DataMember]
        public string SubjectIconBackColor { get; set; }

        [DataMember]
        public string SubjectIconForeColor { get; set; }

        [DataMember]
        public string SubjectIconText { get; set; }

        [DataMember]
        public int? SubjectAssetTypeID { get; set; }

        [DataMember]
        public string SubjectAssetTypePath { get; set; }

        [DataMember, Key, Column(Order = 4, TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember, Key, Column(Order = 5)]
        public int ObjectID { get; set; }

        [DataMember]
        public Cardinality ObjectCardinality { get; set; }

        [DataMember]
        public string ObjectName { get; set; }

        [DataMember]
        public string ObjectIconBackColor { get; set; }

        [DataMember]
        public string ObjectIconForeColor { get; set; }

        [DataMember]
        public string ObjectIconText { get; set; }

        [DataMember]
        public int ObjectAssetTypeID { get; set; }

        [DataMember]
        public string ObjectAssetTypePath { get; set; }

        [DataMember]
        public int? PredicateID { get; set; }

        [DataMember]
        public string PredicateName { get; set; }

        [DataMember]
        public string PredicateInverse { get; set; }

        [DataMember]
        public PredicateType? PredicateType { get; set; }

        public bool IsSystem { get; set; }
    }
}
