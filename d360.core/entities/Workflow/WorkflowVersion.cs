using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;

namespace d360.core.entities.Workflow
{
    [DataContract(Namespace = NAMESPACE), Table("Version", Schema = "workflow")]
    public class WorkflowVersion : BaseIntObject, IIntObject, ICreatedMetadata, IUpdatedMetadata, IUIDMetadata
    {
        [DataMember]
        public int TypeID { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime? CreatedOn { get; set; }

        public int? UpdatedBy { get; set; }

        public DateTime? UpdatedOn { get; set; }

        [DataMember]
        public int Version { get; set; }

        [IgnoreDataMember, ForeignKey("TypeID")]
        public virtual Type Type { get; set; }

        [IgnoreDataMember, ForeignKey("VersionID")]
        public virtual ICollection<WorkflowVersionStep> Steps { get; set; }

        [DataMember]
        public Guid? UID { get; set; }
    }
}
