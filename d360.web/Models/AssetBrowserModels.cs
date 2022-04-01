using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using d360.core.enums;

using Newtonsoft.Json;

namespace d360.web.Models
{
    #region Alert

    public class AssetBrowserAlertRequest
    {
        public List<AssetBrowserAlertAssetRequest> assets { get; set; } = new List<AssetBrowserAlertAssetRequest>();
    }

    public class AssetBrowserAlertAssetRequest
    {
        public Guid uid { get; set; }
    }

    internal class AssetBrowserAlert
    {
        public Guid uid { get; set; }

        public AssetBrowserAlertAsset asset { get; set; }

        public AssetBrowserAlertAction action { get; set; }

        public AssetBrowserAlertScore score { get; set; }
    }

    internal class AssetBrowserAlertAction
    {
        public string name { get; set; }

        public string description { get; set; }
    }

    internal class AssetBrowserAlertAsset
    {
        public Guid uid { get; set; }

        public string icon { get; set; }

        public string displayValue { get; set; }
    }

    internal class AssetBrowserAlertScore
    {
        public ScoreType type { get; set; }

        public string name { get; set; }

        public float value { get; set; }

        public string backColor { get; set; }
    }

    public class AddWorkFlowAction
    {
        public Nullable<Guid> Uid { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
    }

    #endregion

    #region Asset

    public class AssetBrowserResponseModel
    {
        public List<AssetBrowserNode> nodes { get; set; }

        public List<AssetBrowserLink> links { get; set; }

        public List<AssetBrowserHeirarchy> hierarchy { get; set; }

        public List<AssetBrowserRevealNode> reveals { get; set; }

        public bool dataLimitReached { get; set; } = false;
    }

    public class AssetBrowserNodeOwnerCount
    {
        public string key { get; set; }

        public bool expanded { get; set; }

        public string id { get; set; }

        public int? count { get; set; }

        public int responsibilityTypeId { get; set; }

        public string responsibilityType { get; set; }
    }

    public class AssetBrowserNodeRelationCount
    {
        public string key { get; set; }

        public string predicate { get; set; }

        public int predicateId { get; set; }

        public Guid predicateUid { get; set; }

        public int direction { get; set; }

        public int count { get; set; }

        public bool expanded { get; set; }
    }

    public class AssetBrowserHeirarchy
    {
        public string hierarchyKey { get; set; }

        public int backwardReveal { get; set; }

        public int forwardReveal { get; set; }

        [JsonIgnore]
        public string ownersJson { get; set; }

        public List<AssetBrowserNodeOwnerCount> owners => JsonConvert.DeserializeObject<List<AssetBrowserNodeOwnerCount>>(ownersJson ?? "[]");

        [JsonIgnore]
        public string relationsJson { get; set; }

        public List<AssetBrowserNodeRelationCount> relations => JsonConvert.DeserializeObject<List<AssetBrowserNodeRelationCount>>(relationsJson ?? "[]");

        public string predictableId { get; set; }
    }

    public class AssetBrowserRevealNode
    {
        public string hierarchyKey { get; set; }

        public string from { get; set; }

        public string to { get; set; }

        public AssetBrowserApiHopDirection direction { get; set; }
    }

    public class AssetBrowserNode
    {
        public string hierarchyKey { get; set; }

        public bool focal { get; set; }

        public bool leaf { get; set; }

        public string key { get; set; }

        public string group { get; set; }

        public Guid? assetUid { get; set; }

        public int assetId { get; set; }

        public int assetTypeId { get; set; }

        public Guid assetTypeUid { get; set; }

        public decimal backAmount { get; set; }

        public string back { get; set; }

        public string icon { get; set; }

        public AssetTypeClass @class { get; set; }

        public string text { get; set; }

        public int actionCount { get; set; }

        public bool useAsTransformation { get; set; }

        public bool hasResponsibilityReadAccess { get; set; }

        public bool hasAssetReadAccess { get; set; }

        public bool isSubjectInTransformation { get; set; }
    }

    public class AssetBrowserChildLink
    {
        public long id { get; set; }

        public string from { get; set; }

        public string to { get; set; }
    }

    public class AssetBrowserLink
    {
        public string from { get; set; }

        public string to { get; set; }

        public string back { get; set; }

        public int predicateId { get; set; }

        public Guid predicateUid { get; set; }

        public string text { get; set; }

        public int predicateType { get; set; }

        [JsonIgnore]
        public string linksJson { get; set; }

        public List<AssetBrowserChildLink> links => JsonConvert.DeserializeObject<List<AssetBrowserChildLink>>(linksJson ?? "[]");
    }

    public enum AssetBrowserAncestry
    {
        AllAncestors = 1,
        DirectAncestor = 2,
        TypeOnly = 3 //For Impact
    }

    public class AssetBrowserInitialModel
    {
        public AssetBrowserAncestry ancestry { get; set; }

        public Guid uid { get; set; }

        public int hopCount { get; set; }

        public bool includeNonLeaf { get; set; } = true;

        public bool includeDescendantAssets { get; set; } = true;
    }

    public class AssetBrowserImpactInitialModel
    {
        public Guid uid { get; set; }

        public int hopCount { get; set; }

        public bool includeNonLeaf { get; set; } = true;
    }

    public class AssetBrowserLineageInitialModel
    {
        public AssetBrowserAncestry ancestry { get; set; }

        public Guid uid { get; set; }

        public int hopCount { get; set; }

        public bool includeNonLeaf { get; set; } = true;

        public bool includeDescendantAssets { get; set; } = true;
    }

    public abstract class AssetBrowserHopModelBase
    {
        public string hierarchyKey { get; set; }
    }

    public abstract class AssetBrowserHopModelRelationBase : AssetBrowserHopModelBase
    {
        public AssetBrowserAncestry ancestry { get; set; }

        public List<AssetBrowserApiHopAssetRequestModel> assets { get; set; }

        public List<long> preloadedIntersects { get; set; }

        public AssetBrowserApiHopDirection direction { get; set; }

        public bool includeNonLeaf { get; set; } = true;

        public bool includeDescendantAssets { get; set; } = true;
    }

    public class AssetBrowserLineageHopModel : AssetBrowserHopModelRelationBase
    {
    }

    public class AssetBrowserImpactHopModel : AssetBrowserHopModelRelationBase
    {
        public Guid predicateUid { get; set; }
    }

    #endregion

    #region Filter

    internal class AssetBrowserAssetTypeFilterItem
    {
        public Guid Uid { get; set; }

        public int AssetTypeId { get; set; }

        public int ClassId { get; set; }

        public string Class => ((AssetTypeClass)ClassId).GetDisplayName();

        public string Name { get; set; }

        public string Path { get; set; }
    }

    internal class AssetBrowserPredicateFilterItem
    {
        public int Id { get; set; }

        public Guid Uid { get; set; }

        public int TypeId { get; set; }

        public string Type => ((PredicateType)TypeId).GetDisplayName();

        public string Name { get; set; }

        public string Inverse { get; set; }
    }

    internal class AssetBrowserResponsibilityTypeFilterItem
    {
        public int Id { get; set; }

        public Guid Uid { get; set; }

        public string Name { get; set; }
    }

    #endregion

    #region Info
    
    internal class AssetBrowserDiagramAsset
    {
        public string TypeName { get; set; }

        public AssetTypeClass AssetTypeClass { get; set; }

        public string AssetTypeClassDisplayName => AssetTypeClass.GetDisplayName();

        public Guid Uid { get; set; }

        public string DisplayValue { get; set; }

        public string Path { get; set; }

        public string Url { get; set; }

        public List<AssetBrowserDiagramAssetField> Fields { get; set; } = new List<AssetBrowserDiagramAssetField>();

        public List<AssetBrowserDiagramAssetScore> Scores { get; set; } = new List<AssetBrowserDiagramAssetScore>();

        public List<AssetBrowserDiagramAssetOwner> Owners { get; set; } = new List<AssetBrowserDiagramAssetOwner>();
    }

    internal class AssetBrowserDiagramAssetField
    {
        public string Name { get; set; }

        public string Value { get; set; }

        public string Values { get; set; }

        public string Type { get; set; }
    }

    internal class AssetBrowserDiagramAssetScore
    {
        public Guid AssetUid { get; set; }

        public DateTime EffectiveDate { get; set; }

        public decimal Value { get; set; }

        public DateTime? RunDate { get; set; }

        public DateTime? EndDate { get; set; }

        public ScoreType ScoreType { get; set; } = ScoreType.Governance;

        public int LowerThreshold { get; set; }

        public int UpperThreshold { get; set; }
    }

    internal class AssetBrowserDiagramAssetOwner
    {
        public int ResponsibilityTypeID { get; set; }

        public string ResponsibilityTypeName { get; set; }

        public int ResourceID { get; set; }

        public string ResourceName { get; set; }
    }

    #endregion

    #region Hop

    internal class HopNodeResult
    {
        public bool isFocal { get; set; } = false;

        public Guid assetUid { get; set; }

        public long assetID { get; set; }

        public string key { get; set; }

        public long parentID { get; set; }

        public string parentKey { get; set; }

        public string back { get; set; }

        public string fore { get; set; }

        public string icon { get; set; }

        public int assetTypeID { get; set; }

        public Guid assetTypeUid { get; set; }

        public AssetTypeClass @class { get; set; }

        public string displayValue { get; set; }

        public AssetBrowserApiHopDirection reveal { get; set; }

        public int actionCount { get; set; }

        public string ownerCounts { get; set; }

        public string relationCounts { get; set; }

        public bool useAsTransformation { get; set; }

        public bool hasAssetReadAccess { get; set; }

        public bool isSubjectInTransformation { get; set; }

        public bool isLeaf { get; set; }
    }

    internal class HopLinkResult
    {
        public Guid uid { get; set; }

        public AssetBrowserApiHopDirection direction { get; set; }

        public string subjectKey { get; set; }

        public string objectKey { get; set; }

        public int predicateId { get; set; }

        public Guid predicateUid { get; set; }

        public string predicate { get; set; }

        public PredicateType predicateType { get; set; }
    }

    internal class HopModel
    {
        public List<HopNodeResult> nodes { get; set; }

        public List<HopLinkResult> links { get; set; }
    }

    #endregion

    #region Owner

    [DataContract]
    public class AssetBrowserOwnerModel
    {
        [DataMember]
        public string key { get; set; }

        [DataMember]
        public string displayValue { get; set; }
       
        [DataMember]
        public string backColor { get; set; }
        
        [DataMember]
        public string foreColor { get; set; }

        [DataMember]
        public string icon { get; set; }

        public Guid assetUid { get; set; }

        [DataMember]
        public Guid resourceUid { get; set; }

        public int resourceId { get; set; }
    }

    public class AssetBrowserOwnersModel
    {
        public ICollection<AssetBrowserOwnerModel> owners { get; set; }

        public ICollection<AssetBrowserOwnerRelationModel> ownerRelations { get; set; }
    }

    public class AssetBrowserOwnerRelationModel
    {
        public Guid assetUid { get; set; }

        public Guid ownerUid { get; set; }

        public string assetKey { get; set; }

        public string ownerKey { get; set; }

        public string backColor { get; set; }

        public string foreColor { get; set; }
    }

    #endregion
}
