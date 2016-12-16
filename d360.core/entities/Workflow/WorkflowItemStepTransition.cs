using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Workflow
{
    [DataContract(Namespace = NAMESPACE), Table("ItemStepTransition", Schema = "workflow")]
    public class WorkflowItemStepTransition : BaseObject
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember, Key, Column(Order = 1)]
        public int FromItemStepID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int ToItemStepID { get; set; }
        
        [IgnoreDataMember]
        public string Condition { get; set; }

        [IgnoreDataMember]
        public DateTime Date { get; set; }

        [IgnoreDataMember, ForeignKey("FromItemStepID")]
        public virtual WorkflowItemStep FromItemStep { get; set; }

        [IgnoreDataMember, ForeignKey("ToItemStepID")]
        public virtual WorkflowItemStep ToItemStep { get; set; }
    }
}
