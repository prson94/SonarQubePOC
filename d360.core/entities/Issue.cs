using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Issue : BaseIntObject, IIntObject, IUpdatedMetadata
    {
        public int IssueTypeID { get; set; }

        [DataMember]
        public int? CreatedBy { get; set; }

        [DataMember]
        public DateTime? CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public int? UpdatedBy { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string ObjectType { get; set; }

        [DataMember]
        public int ObjectTypeID { get; set; }

        public virtual IssueType IssueType { get; set; }
    }
}