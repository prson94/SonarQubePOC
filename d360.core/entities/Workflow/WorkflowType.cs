using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;
using d360.core.enums;

namespace d360.core.entities.Workflow
{
    [DataContract(Namespace = NAMESPACE), Table("Type", Schema = "workflow")]
    public class Type : BaseIntObject, IIntObject, ICreatedMetadata, IUpdatedMetadata, IUIDMetadata
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime? CreatedOn { get; set; }

        public int? UpdatedBy { get; set; }

        public DateTime? UpdatedOn { get; set; }

        [DataMember]
        public int? PublishedVersionID { get; set; }

        [DataMember]
        public State State { get; set; } = State.Unknown;

        [DataMember]
        public Guid? UID { get; set; }
    }
}
