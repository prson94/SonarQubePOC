using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [JsonArray]
    [DataContract(Name="assets")]
    public class AssetInserts : List<AssetInsert>
    {

    }

    [DataContract(Name = "asset")]
    public class AssetInsert
    {
        [DataMember]
        public Guid? ParentUid { get; set; }

        [DataMember]
        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();
    }

    [JsonArray]
    [DataContract(Name = "assets")]
    public class AssetUpdates : List<AssetUpdate>
    {

    }

    [DataContract(Name = "asset")]
    public class AssetUpdate// : Dictionary<string, string>
    {
        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();
    }

    public class AssetImportResult
    {
        public int ItemNumber { get; set; }
        public string SourceID { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
        public bool IsNew { get; set; }
        public int ObjectID { get; set; }
        public Guid uid { get; set; }
    }

    [DataContract]
    public class DatabaseBulkAssetResult
    {
        [DataMember]
        public int ItemNumber { get; set; }
        [DataMember]
        public Guid uid { get; set; }

        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public bool Success { get; set; }
        
        public bool IsNew { get; set; }
        public string Object { get; set; }
        public int ObjectID { get; set; }        
    }
}
