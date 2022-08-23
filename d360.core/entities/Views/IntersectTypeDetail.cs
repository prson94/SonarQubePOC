using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class IntersectTypeDetail : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int ID { get; set; }

        [DataMember]
        public Guid Uid { get; set; }

		[DataMember]
		public string Name { get; set; }

		public AssetTypeClass SubjectClass { get; set; }

        [DataMember]
        public int? SubjectAssetTypeID { get; set; }

		[DataMember]
		public Guid? SubjectUid { get; set; }

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
        public string SubjectAssetTypePath { get; set; }

        [DataMember]
        public AssetTypeClass ObjectClass { get; set; }

		[DataMember]
		public int? ObjectAssetTypeID { get; set; }

		[DataMember]
		public Guid? ObjectUid { get; set; }

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
