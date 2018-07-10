using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("Execution", Schema = "integration")]
    public class IntegrationExecution : BaseLongObject
    {
        [DataMember]
        public DateTime StartedOn { get; set; }

        [DataMember]
        public DateTime? CompletedOn { get; set; }

        [DataMember]
        public bool Archived { get; set; }

        [IgnoreDataMember, ForeignKey("ExecutionID")]
        public virtual ICollection<IntegrationExecutionAssetType> ExecutionAssetTypes { get; set; }
    }
}
