using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [JsonArray]
    [DataContract(Name="assets")]
    public class BulkAssetImport : List<Dictionary<string, string>>
    {

    }

    public class AssetImportResult
    {
        public int ItemNumber { get; set; }
        public string SourceID { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
        public bool IsNew { get; set; }
        public int ObjectID { get; set; }
    }

    [JsonArray]
    [DataContract(Name = "relationships")]
    public class BulkRelationshipImport : List<RelationshipImportRequest>
    {

    }

    public class RelationshipImportRequest
    {
        public int ItemNumber { get; set; }
        public string SubjectSourceID { get; set; }
        public string ObjectSourceID { get; set; }
        public int? PredicateType { get; set; }
        public int IntersectTypeID { get; set; }

        public string Message { get; set; }
        public bool Success { get; set; }
    }

    public class BulkOwnerImport
    {
        public string UserIdFieldName { get; set; }

        public IList<OwnerImportRequest> Items { get; set; }
    }

    public class OwnerImportRequest
    {
        public int ItemNumber { get; set; }
        public string SourceID { get; set; }
        public string RoleName { get; set; }
        public string UserId { get; set; }

        public string Message { get; set; }
        public bool Success { get; set; }
    }

    [DataContract]
    public class DatabaseBulkAssetResult
    {
        [DataMember]
        public int ItemNumber { get; set; }
        public int ObjectID { get; set; }
        [DataMember]
        public string SourceID { get; set; }
        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public bool Success { get; set; }
        [DataMember]
        public bool IsNew { get; set; }
    }
}
