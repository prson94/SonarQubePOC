using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("Execution", Schema = "api")]
    public class ApiExecution : BaseObject
    {
        [DataMember, Key]
        public Guid ExecutionID { get; set; }

        [DataMember]
        public int Total { get; set; }

        [DataMember]
        public int Processed { get; set; }

        [DataMember]
        public int Error { get; set; }

        [DataMember]
        public int ResourceID { get; set; }

        [DataMember]
        public string Fields { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public DateTime StartedOn { get; set; }

        [DataMember]
        public DateTime? CompletedOn { get; set; }
    }

    public class ApiExecutionFields_PostAssets
    {
        public Guid AssetTypeUid { get; set; }
    }

    public class ApiExecutionFields_PutAssets
    {
        public Guid AssetTypeUid { get; set; }
    }

    public class ApiExecutionFields_DeleteAssets
    {
        public Guid AssetTypeUid { get; set; }
    }

    public class ApiExecutionFields_PostRelationships
    {
        public Guid IntersectTypeUid { get; set; }
    }
}
