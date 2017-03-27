using d360.core.entities.Contracts;
using d360.core.enums.Workflow;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Workflow
{
    [DataContract(Namespace = NAMESPACE), Table("EventRegistration", Schema = "workflow")]
    public class WorkflowEventRegistration : BaseIntObject, IIntObject
    {
        [DataMember]
        public int TypeID { get; set; }

        [DataMember]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public ChangeType ChangeType { get; set; }

        [DataMember]
        public string Condition { get; set; }

        [NotMapped]
        public dynamic ConditionObject { get; set; }

        [IgnoreDataMember, ForeignKey("TypeID")]
        public virtual Type Type { get; set; }
    }
}
