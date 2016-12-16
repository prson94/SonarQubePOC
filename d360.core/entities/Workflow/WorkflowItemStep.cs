using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Workflow
{
    [DataContract(Namespace = NAMESPACE), Table("ItemStep", Schema = "workflow")]
    public class WorkflowItemStep : BaseObject
    {
        [DataMember]
        public long ID { get; set; }

        [DataMember]
        public long ItemID { get; set; }

        [DataMember]
        public int StepID { get; set; }

        [IgnoreDataMember]
        public string Settings { get; set; }

        [IgnoreDataMember]
        public string Fields { get; set; }

        [DataMember]
        public DateTime Date { get; set; }

        [IgnoreDataMember, ForeignKey("ItemID")]
        public virtual WorkflowItem Item { get; set; }

        [IgnoreDataMember, ForeignKey("StepID")]
        public virtual WorkflowVersionStep Step { get; set; }
    }
}
