using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;

namespace d360.core.entities.Workflow
{
    [DataContract(Namespace = NAMESPACE), Table("Item", Schema = "workflow")]
    public class WorkflowItem : BaseObject, IUIDMetadata
    {
        [DataMember]
        public long ID { get; set; }

        [DataMember]
        public int VersionID { get; set; }

        [DataMember]
        public bool Active { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public int StartedBy { get; set; }

        [DataMember]
        public DateTime StartedOn { get; set; }

        [DataMember]
        public int UpdatedBy { get; set; }

        [DataMember]
        public DateTime UpdatedOn { get; set; }

        [DataMember]
        public int? CompletedBy { get; set; }

        [DataMember]
        public DateTime? CompletedOn { get; set; }

        [DataMember]
        public bool IsTest { get; set; }

        [DataMember]
        public int NumberOfEvents { get; set; }

        [IgnoreDataMember, ForeignKey("VersionID")]
        public virtual WorkflowVersion Version { get; set; }

        [IgnoreDataMember, ForeignKey("ItemID")]
        public virtual ICollection<WorkflowItemStep> Steps { get; set; }

        [DataMember]
        public Guid? UID { get; set; }
    }
}
