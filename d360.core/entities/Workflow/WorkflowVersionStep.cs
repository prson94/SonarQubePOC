using d360.core.entities.Contracts;
using d360.core.enums.Workflow;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Workflow
{
    [DataContract(Namespace = NAMESPACE), Table("VersionStep", Schema = "workflow")]
    public class WorkflowVersionStep : BaseIntObject, IIntObject
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int VersionID { get; set; }

        [DataMember]
        public int? ParentID { get; set; }

        [DataMember]
        public StepType StepType { get; set; }

        [DataMember]
        public int ActivityType { get; set; }

        [IgnoreDataMember]
        public string Settings { get; set; }

        [IgnoreDataMember]
        public string Fields { get; set; }

        [DataMember]
        public int XPosition { get; set; }

        [DataMember]
        public int YPosition { get; set; }

        [IgnoreDataMember, ForeignKey("VersionID")]
        public virtual WorkflowVersion Version { get; set; }
    }
}
