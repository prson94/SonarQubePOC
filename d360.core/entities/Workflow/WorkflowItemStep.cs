using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Xml.Linq;

using d360.core.entities.Contracts;

namespace d360.core.entities.Workflow
{
    [DataContract(Namespace = NAMESPACE), Table("ItemStep", Schema = "workflow")]
    public class WorkflowItemStep : BaseObject, IUIDMetadata
    {
        [DataMember]
        public long ID { get; set; }

        [DataMember]
        public long ItemID { get; set; }

        [DataMember]
        public int StepID { get; set; }

        [IgnoreDataMember]
        public string Settings { get; set; }

        [NotMapped, IgnoreDataMember]
        public XElement SettingsDocument { get { return XElement.Parse(Settings); } }

        [IgnoreDataMember]
        public string Fields { get; set; }

        [NotMapped, IgnoreDataMember]
        public XElement FieldsDocument { get { return XElement.Parse(Fields); } }

        [DataMember]
        public int StartedBy { get; set; }

        [DataMember]
        public DateTime StartedOn { get; set; }

        [DataMember]
        public int? CompletedBy { get; set; }

        [DataMember]
        public DateTime? CompletedOn { get; set; }

        [IgnoreDataMember, ForeignKey("ItemID")]
        public virtual WorkflowItem Item { get; set; }

        [IgnoreDataMember, ForeignKey("StepID")]
        public virtual WorkflowVersionStep Step { get; set; }

        [DataMember]
        public Guid? UID { get; set; }
    }
}
