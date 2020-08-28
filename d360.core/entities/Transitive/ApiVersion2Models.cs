using d360.core.enums;
using d360.core.enums.Workflow;
using d360.core.resources;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

    public class AssetTypeUpsert
    {
        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        [MaxLength(250, ErrorMessageResourceType = typeof(AssetTypeErrors), ErrorMessageResourceName = "MaxLengthExceeded")]
        public string Name { get; set; }

        [DataMember]
        [JsonConverter(typeof(EnumConverter))]
        public AssetTypeClass Class { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public bool AutoDisplayDescription { get; set; }

        [DataMember]
        [MaxLength(250, ErrorMessageResourceType = typeof(AssetTypeErrors), ErrorMessageResourceName = "MaxLengthExceeded")]
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

        [DataMember]
        public bool? CanOwnFusion { get; set; }
        [DataMember]
        public int? FusionID { get; set; }
        [DataMember]
        public bool? AutoDisplayParent { get; set; }
        [DataMember]
        public FlowObjectType? FlowObjectType { get; set; }
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
        [DataMember]
        public string Icon { get; set; }
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

    public class AssetPathResult
    {
        public string path { get; set; }
        public Guid uid { get; set; }
    }

    public class AssetPathResults
    {
        public IEnumerable<AssetPathResult> items { get; set; }
        public int? total { get; set; }
    }


    [JsonArray]
    [DataContract(Name = "assets")]
    public class AssetDeletes : List<AssetDelete>
    {

    }

    [DataContract(Name = "asset")]
    public class AssetDelete : IExecutionItem
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

    [DataContract]
    public class AssetTypeSingleDelete
    {
        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public bool Cascade { get; set; }
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
    public class DatabaseBulkAssetResult : IWorkflowEnabledAsset, IGraphAsset
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
        [DataMember]
        public string Owner { get; set; }
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


    public class RelationshipTypeResult
    {
        [DataMember]
        public Guid uid { get; set; }
        [DataMember]
        public Guid? ExecutionItemUid { get; set; }

        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public bool Success { get; set; }
    }


    public class RelationshipUidResultItem
    {
        [DataMember]
        public Guid RelationshipUid { get; set; }
        [DataMember]
        public Guid? SubjectUid { get; set; }

        [DataMember]
        public Guid? ObjectUid { get; set; }
        [DataMember]
        public string Owner { get; set; }
    }

    public class RelationshipUidResult
    {
        [DataMember]
        public IEnumerable<RelationshipUidResultItem> Results { get; set; }
        [DataMember]
        public int? Total { get; set; }
    }

    public class RelationshipTypeInsert
    {
        [DataMember]
        public Guid? ExecutionItemUid { get; set; }
        [DataMember]
        public Guid? Uid { get; set; }
        [DataMember]
        public Guid PredicateUid { get; set; }
        [DataMember]
        public Guid SubjectUid { get; set; }
        [DataMember]
        public Cardinality SubjectCardinality { get; set; }
        [DataMember]
        public Guid ObjectUid { get; set; }
        [DataMember]
        public Cardinality ObjectCardinality { get; set; }
    }

    public class RelationshipTypeUpdate
    {
        [DataMember]
        public Guid? ExecutionItemUid { get; set; }

        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public Guid PredicateUid { get; set; }
        [DataMember]
        public Cardinality SubjectCardinality { get; set; }

        [DataMember]
        public Cardinality ObjectCardinality { get; set; }
    }

    public class RelationshipTypeDelete
    {
        [DataMember]
        public Guid? ExecutionItemUid { get; set; }

        [DataMember]
        public Guid Uid { get; set; }

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
    public class DatabaseBulkRelationshipResult : IWorkflowEnabledAsset, IGraphAsset
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

    [DataContract]
    public class PredicateApiResult
    {
        [DataMember]
        public Guid? ExecutionItemUid { get; set; }

        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public bool Success { get; set; }
    }

    [JsonArray]
    [DataContract(Name = "predicates")]
    public class PredicateDeletes : List<PredicateDelete> { }

    public class PredicateDelete
    {
        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public Guid? ExecutionItemUid { get; set; }
    }

    [DataContract]
    public class PredicateDeleteResult : PredicateApiResult { }

    [JsonArray]
    [DataContract(Name = "predicates")]
    public class PredicateUpserts : List<PredicateUpsert> { }

    public class PredicateUpsert
    {
        [DataMember]
        public Guid? ExecutionItemUid { get; set; }

        [DataMember]
        public PredicateType Type { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Inverse { get; set; }

        [DataMember]
        public Guid? Uid { get; set; }
    }

    [DataContract]
    public class PredicateUpsertResult : PredicateApiResult { }



    public class AssetFieldTypeUpdate
    {
        public string Object { get; set; }
        public int ObjectId { get; set; }
        public int Id { get; set; }
    }

    [DataContract]
    public class ResponsibilityTypeUpsertResult
    {
        [DataMember]
        public int ItemNumber { get; set; }

        [DataMember]
        public Guid? ExecutionItemUid { get; set; }

        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public bool Success { get; set; }
    }

    [DataContract]
    public class ResponsibilityTypeInsertModel
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }
    }

    [DataContract]
    public class ResponsibilityTypeUpsertModel
    {
        [DataMember]
        public Guid? Uid { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

    }

    [DataContract]
    public class ResponsibilityTypeDeleteResult
    {
        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public bool Success { get; set; }
    }

    [DataContract]
    public class ResponsibilityTypeDeleteModel
    {
        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public bool Cascade { get; set; }

    }

    #region Allocations

    [DataContract]
    public class ResponsibilityTypeAllocationInsertModel
    {
        [DataMember]
        public Guid AssetTypeUid { get; set; }

        [DataMember]
        public List<int> Permissions { get; set; }
    }

    [DataContract]
    public class ResponsibilityTypeAllocationResponseModel
    {
        [DataMember]
        public Guid AssetTypeUid { get; set; }

        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public bool Success { get; set; }
    }

    [DataContract]
    public class ResponsibilityTypeAllocationDeleteModel
    {
        [DataMember]
        public bool Cascade { get; set; }
        [DataMember]
        public List<ResponsibilityTypeAllocationDeleteItemModel> Items { get; set; }
    }

    [DataContract]
    public class ResponsibilityTypeAllocationDeleteItemModel
    {
        [DataMember]
        public Guid AssetTypeUid { get; set; }
    }
    #endregion

    [DataContract]
    public class ResponsibilityOverridePostModel
    {
        [DataMember]
        public List<Guid> ResourceUid { get; set; }
        [DataMember]
        public string Description { get; set; }
    }

    [DataContract]
    public class ResponsibilityOverrideDeleteModel
    {
        [DataMember]
        public Guid ResourceUid { get; set; }
    }

    public class SecurityAssetModel
    {
        public Guid uid { get; set; }
        public int SecurityAssetId { get; set; }
        public string SecurityAsset { get; set; }
        public bool Exists { get; set; }
    }

    [DataContract]
    public class ResponsibilityRuleUpsertModel
    {
        [DataMember]
        public Guid? ExecutionItemUid { get; set; }
        [DataMember]
        public Guid? AssetTypeUid { get; set; }
        [DataMember]
        public Guid? Uid { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public bool IsVisible { get; set; }
        [DataMember]
        public bool ApplyToType { get; set; }
        [DataMember]
        public string Context { get; set; }
        [DataMember]
        public RuleDefinition Definition { get; set; }
    }
    [DataContract]
    public class RuleDefinition
    {
        [DataMember]
        public List<RuleWhen> When { get; set; }
        [DataMember]
        public List<RuleThenWrapper> Then { get; set; }

    }
    [DataContract]
    public class RuleThenWrapper
    {
        [DataMember]
        public Guid? AssigneeTypeUid { get; set; }
        [DataMember]
        public List<RuleThen> Conditions { get; set; } = new List<RuleThen>();
    }

    [DataContract]
    public class RuleThen
    {
        [DataMember]
        public RuleFieldCondition Field { get; set; }
        [DataMember]
        public RuleAssigneeCondition Assignee { get; set; }
    }

    [DataContract]
    public class RuleWhen
    {
        [DataMember]
        public RuleFieldCondition Field { get; set; }
        [DataMember]
        public RuleRelationCondition Relation { get; set; }
    }

    [DataContract]
    public class RuleFieldCondition
    {
        [DataMember]
        public string ApiName { get; set; }
        [DataMember]
        public string Value { get; set; }
    }

    [DataContract]
    public class RuleAssigneeCondition
    {
        [DataMember]
        public Guid? Uid { get; set; }
    }


    [DataContract]
    public class RuleRelationCondition
    {
        [DataMember]
        public Guid? IntersectTypeUid { get; set; }
        [DataMember]
        public Guid? AssetUid { get; set; }
    }

    [DataContract]
    public class ResponsibilityRuleUpsertResponseModel
    {
        [DataMember]
        public int ItemNumber { get; set; }
        [DataMember]
        public Guid? Uid { get; set; }
        [DataMember]
        public Guid? ExecutionItemUid { get; set; }
        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public bool Success { get; set; }
    }

    [DataContract]
    public class ResponsibilityRuleDeleteModel
    {
        [DataMember]
        public Guid Uid { get; set; }
    }

    [DataContract]
    public class ResponsibilityRuleDeleteResponse
    {
        [DataMember]
        public Guid? Uid { get; set; }
        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public bool Success { get; set; }
    }

    public class UpsertModel
    {
        public Guid AssetTypeUid { get; set; }
        public List<UpsertAsset> Assets { get; set; }
    }
    public class UpsertAsset
    {
        public Guid? Uid { get; set; }
        public Dictionary<string, string> Fields { get; set; }
        [JsonIgnore]
        public Guid? ExternalKey { get; set; }
    }

    public class FieldValidationFieldProperties
    {
        public bool ContainsColorField { get; set; }
        public int JsonFieldCount { get; set; }
    }

}
