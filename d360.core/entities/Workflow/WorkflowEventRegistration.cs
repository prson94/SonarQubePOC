using d360.core.entities.Contracts;
using d360.core.enums;
using d360.core.enums.Workflow;
using System;
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

        [DataMember, NotMapped]
        public dynamic ConditionObject { get; set; }

        [DataMember]
        public string Settings { get; set; }

        [DataMember]
        public DateTime? LastExecuted{ get; set; }

        [DataMember]
        public State State { get; set; } = State.Unknown;

        [DataMember, NotMapped]
        public dynamic SettingsObject { get; set; }

        [IgnoreDataMember, ForeignKey("TypeID")]
        public virtual Type Type { get; set; }
    }
}
