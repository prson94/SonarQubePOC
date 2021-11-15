using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Net;
using d360.core.enums;

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

        [DataMember]
        public string Method { get; set; }

        [DataMember]
        public string Route { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime? ProcessingStartedOn { get; set; }

        [DataMember]
        public bool MarkedForProcessing { get; set; } = false;

        [DataMember]
        public State State { get; set; } = State.Active;

        [DataMember]
        public string ApplicationId { get; set; }
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

    public class ApiExecutionFields_PutRelationships
    {
        public Guid IntersectTypeUid { get; set; }
    }

    public class ApiExecutionFields_DeleteRelationships
    {
        public Guid IntersectTypeUid { get; set; }
    }

    public class ApiExecutionFields_DeleteAssetTypes
    {
        public Guid? AssetTypeUid { get; set; }
    }

    public class ApiExecutionFields_DeletePredicates
    {
        public Guid PredicateUid { get; set; }
    }


    public class APIExecutionAPIModelResult : PagedApiBaseViewModel
    {
        [DataMember]
        public IEnumerable<APIExecutionAPIModel> items { get; set; }

        [IgnoreDataMember]
        public HttpStatusCode StatusCode { get; set; }
        [IgnoreDataMember]
        public string Message { get; set; }
    }

    public class APIExecutionAPIModel
    {
        public Guid ExecutionID { get; set; }
        public Guid ResourceUid { get; set; }
        public string Resource { get; set; }
        public int Total { get; set; }
        public int Processed { get; set; }
        public int Error { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime? ProcessingStartedOn { get; set; }
        public DateTime? StartedOn { get; set; }
        public DateTime? CompletedOn { get; set; }
        public string Method { get; set; }
        public string Route { get; set; }
        public dynamic Fields { get; set; }
        public string ApplicationId { get; set; }
    }
}
