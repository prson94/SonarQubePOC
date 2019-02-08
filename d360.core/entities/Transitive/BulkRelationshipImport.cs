using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [JsonArray]
    [DataContract(Name = "relationships")]
    public class RelationshipInserts : List<RelationshipInsert>
    {

    }

    
    public class RelationshipInsert
    {
        [DataMember]
        public Guid SubjectAssetUid { get; set; }
        [DataMember]
        public Guid ObjectAssetUid { get; set; }
        [DataMember]
        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();
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

    [DataContract]
    public class DatabaseBulkRelationshipResult
    {
        [DataMember]
        public Guid ExecutionID { get; set; }

        [DataMember]
        public int ItemNumber { get; set; }

        [DataMember]
        public int IntersectID { get; set; }

        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public bool Success { get; set; }
        [DataMember]
        public bool IsNew { get; set; }
    }
}
