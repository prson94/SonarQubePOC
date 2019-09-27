using d360.core.enums;
using d360.core.enums.Workflow;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    public interface IWorkflowEnabledAsset
    {
        ChangeType ChangeType { get; }
        string Object { get; set; }
        int ObjectID { get; set; }
        bool Success { get; set; }
    }

    public interface IGraphAsset
    {
        bool Success { get; set; }
        Guid uid { get; set; }
        string Object { get; set; }
    }

    public interface IAssetUpsert
    {
        Guid Uid { get; set; }

        Guid? ExecutionItemUid { get; set; }

        Guid? ParentUid { get; set; }

        Dictionary<string, string> Fields { get; set; }
    }

    public interface IExecutionItem
    {
        Guid? ExecutionItemUid { get; set; }
    }

    public class AssetTypeInsert
    {
        [DataMember]
        public Guid Uid { get; set; }
        
        [DataMember]
        public string Name { get; set; }
        
        [DataMember]
        [JsonConverter(typeof(StringEnumConverter))]
        public AssetTypeClass Class { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public bool AutoDisplayDescription { get; set; }
        [DataMember]
        public string DisplayFormat { get; set; }
        public HierarchyInsert Hierarchy { get; set; }
        
        public IconStyleInsert IconStyle { get; set; }

        [DataMember]
        public Guid? ParentUid { get; set; }
        [DataMember]
        public string Notes { get; set; }
        [JsonIgnore]
        public int ObjectID { get; set; }
        [JsonIgnore]
        public string Object { get; set; }

        [DataMember]
        public bool UseAsTransformation { get; set; }



    }

    public class AssetTypeSuccess
    {
        [DataMember]
        public Guid Uid { get; set; }
        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public bool Success { get; set; }
    }

    [DataContract(Name = "Hierarchy")]
    public class HierarchyInsert
    {
       [DataMember] public int MaximumDepth { get; set; }
        [DataMember] public Guid? PredicateUid { get; set; }
    }

    [DataContract(Name = "IconStyle")]
    public class IconStyleInsert
    {
        [DataMember]
        public string ForeColor { get; set; }

        [DataMember]
        public string BackColor { get; set; }
    }

    [DataContract(Name = "asset")]
    public class AssetInsert : IAssetUpsert, IExecutionItem
    {
        [IgnoreDataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public Guid? ExecutionItemUid { get; set; }

        [DataMember]
        public Guid? ParentUid { get; set; }

        [DataMember]
        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();
    }

    [DataContract(Name = "asset")]
    public class AssetUpdate : IAssetUpsert, IExecutionItem
    {
        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public Guid? ExecutionItemUid { get; set; }

        [DataMember]
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
    public class AssetDelete: IExecutionItem
    {
        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public Guid? ExecutionItemUid { get; set; }

        [DataMember]
        public bool? Cascade { get; set; }
    }

    [JsonArray]
    [DataContract(Name = "assets")]
    public class AssetTypeDeletes : List<AssetTypeDelete>
    {

    }

    [DataContract(Name = "asset")]
    public class AssetTypeDelete : IExecutionItem
    {
        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public bool Cascade { get; set; }

        [DataMember]
        public Guid? ExecutionItemUid { get; set; }
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
    public class DatabaseBulkAssetResult: IWorkflowEnabledAsset, IGraphAsset
    {
        [DataMember]
        public int ItemNumber { get; set; }
        [DataMember]
        public Guid uid { get; set; }
        [DataMember]
        public Guid? ExecutionItemUid { get; set; }

        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public bool Success { get; set; }

        public bool IsNew { get; set; }
        public string Object { get; set; }
        public int ObjectID { get; set; }

        public ChangeType ChangeType { get { return (IsNew ? ChangeType.Add : ChangeType.Update); } }
    }

    [DataContract]
    public class BulkAssetCrossReferenceResult
    {
       
        [DataMember]
        public int Total { get; set; }

        [DataMember]
        public int Processed { get; set; }
        [DataMember]
        public int Error { get; set; }
        [DataMember]
        public DateTime StartedOn { get; set; }
        [DataMember]
        public DateTime? CompletedOn { get; set; }
        [DataMember]
        public List<AssetCrossReferenceResult> Results { get; set; }

    }

    [DataContract]
    public class AssetCrossReferenceResult
    {
        [DataMember]
        public int ItemNumber { get; set; }

        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public String Message { get; set; }
        [DataMember]
        public bool Success { get; set; }
    }

    [DataContract]
    public class DatabaseBulkAssetTypeResult
    {
        [DataMember]
        public int ItemNumber { get; set; }
        [DataMember]
        public Guid uid { get; set; }
        [DataMember]
        public Guid? ExecutionItemUid { get; set; }

        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public bool Success { get; set; }
    }

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
        public Guid? ExecutionItemUid { get; set; }

        [DataMember]
        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();
    }

    [JsonArray]
    [DataContract(Name = "relationships")]
    public class RelationshipDeletes : List<RelationshipDelete>
    {
    }

    public class RelationshipDelete
    {
        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public Guid? ExecutionItemUid { get; set; }

        [DataMember]
        public bool Cascade { get; set; }

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
    public class DatabaseBulkRelationshipResult: IWorkflowEnabledAsset, IGraphAsset
    {
        public Guid ExecutionID { get; set; }

        [DataMember]
        public Guid? ExecutionItemUid { get; set; }

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

        public ChangeType ChangeType { get { return (IsNew ? ChangeType.Add : ChangeType.Update); } }

        public string Object { get { return "Intersect"; } set { } }

        public int ObjectID { get { return IntersectID; } set { } }

        [DataMember]
        public Guid uid { get; set; }
    }

    public class AssetDataProfileResult
    {
        public Guid AssetUid { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
    }

    public class AssetDataProfileDeleteResult
    {
        public Guid AssetUid { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
    }

    public class AssetDataProfileDelete
    {
        public Guid AssetUid { get; set; }
    }

    public class AssetFieldTypeUpdate
    {
        public string Object { get; set; }
        public int ObjectId { get; set; }
        public int Id { get; set; }
    }
}
