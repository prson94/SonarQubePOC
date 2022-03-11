using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Xml.Linq;

namespace d360.core.entities.Workflow
{
    [DataContract(Namespace = NAMESPACE), Table("ItemStepTransition", Schema = "workflow")]
    public class WorkflowItemStepTransition : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public long FromItemStepID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public long ToItemStepID { get; set; }

        [IgnoreDataMember]
        public string Condition { get; set; }

        [NotMapped, IgnoreDataMember]
        public XElement ConditionDocument { get { return XElement.Parse(Condition); } }

        [IgnoreDataMember]
        public DateTime Date { get; set; }

        [IgnoreDataMember, ForeignKey("FromItemStepID")]
        public virtual WorkflowItemStep FromItemStep { get; set; }

        [IgnoreDataMember, ForeignKey("ToItemStepID")]
        public virtual WorkflowItemStep ToItemStep { get; set; }
    }
}
