using d360.core.entities.Contracts;
using d360.core.enums.Workflow;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Workflow
{
    [DataContract(Namespace = NAMESPACE), Table("VersionStepTransition", Schema = "workflow")]
    public class WorkflowVersionStepTransition : BaseIntObject, IIntObject
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember, Key, Column(Order = 1)]
        public int FromVersionStepID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int ToVersionStepID { get; set; }
        
        [DataMember]
        public TransitionType TransitionType { get; set; }
        
        [IgnoreDataMember]
        public string Condition { get; set; }

        [IgnoreDataMember]
        public LinkType LinkType { get; set; }

        [IgnoreDataMember, ForeignKey("FromVersionStepID")]
        public virtual WorkflowVersionStep FromVersionStep { get; set; }

        [IgnoreDataMember, ForeignKey("ToVersionStepID")]
        public virtual WorkflowVersionStep ToVersionStep { get; set; }
    }
}
