using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Workflow
{
    [DataContract(Namespace = NAMESPACE), Table("ItemAssignment", Schema = "workflow")]
    public class WorkflowItemAssignment : BaseObject
    {
        [DataMember]
        public long ID { get; set; }

        [DataMember]
        public long? ItemStepID { get; set; }

        [DataMember]
        public long ItemID { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(50)]
        public string ResourceObject { get; set; }

        [DataMember]
        public int ResourceObjectID { get; set; }

        [DataMember]
        public int CreatedBy { get; set; }

        [DataMember]
        public DateTime CreatedOn { get; set; }

        [DataMember]
        public int UpdatedBy { get; set; }

        [DataMember]
        public DateTime UpdatedOn { get; set; }
    }
}
