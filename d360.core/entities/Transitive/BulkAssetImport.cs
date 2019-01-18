using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    public interface IAssetUpsert
    {
        Guid Uid { get; set; }

        Guid? ParentUid { get; set; }

        Dictionary<string, string> Fields { get; set; }
    }

    [DataContract(Name = "asset")]
    public class AssetInsert : IAssetUpsert
    {
        [IgnoreDataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public Guid? ParentUid { get; set; }

        [DataMember]
        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();
    }

    [DataContract(Name = "asset")]
    public class AssetUpdate : IAssetUpsert
    {
        [DataMember]
        public Guid Uid { get; set; }

        [IgnoreDataMember]
        public Guid? ParentUid { get; set; }

        [DataMember]
        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();
    }

    [JsonArray]
    [DataContract(Name = "assets")]
    public class AssetDeletes : List<AssetDelete>
    {

    }

    [DataContract(Name = "asset")]
    public class AssetDelete
    {
        [DataMember]
        public Guid Uid { get; set; }
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
