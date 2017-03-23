using d360.core.entities.Contracts;
using d360.core.enums.Workflow;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Xml.Linq;

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
        public d360.core.enums.Workflow.WorkflowActivityType ActivityType { get; set; }

        [IgnoreDataMember]
        public string Settings { get; set; }

        [NotMapped, IgnoreDataMember]
        public XElement SettingsDocument { get { return XElement.Parse(Settings); } }

        [IgnoreDataMember]
        public string Fields { get; set; }

        [NotMapped, IgnoreDataMember]
        public XElement FieldsDocument { get { return XElement.Parse(Fields); } }

        [DataMember]
        public int XPosition { get; set; }

        [DataMember]
        public int YPosition { get; set; }

        [IgnoreDataMember, ForeignKey("VersionID")]
        public virtual WorkflowVersion Version { get; set; }
    }
}
