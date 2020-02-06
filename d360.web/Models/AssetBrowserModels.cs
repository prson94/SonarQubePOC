using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

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

    #endregion

    #region Asset

    [DataContract]
    public class AssetBrowserAssetModel
    {
        [DataMember]
        public int hop { get; set; }
        [DataMember]
        public int assetTypeId { get; set; }
        [DataMember]
        public Guid assetTypeUid { get; set; }
        [DataMember]
        public Guid assetUid { get; set; }
        [DataMember]
        public string key { get; set; }
        [DataMember]
        public string parentKey { get; set; }
        [DataMember]
        public string displayValue { get; set; }
        [DataMember]
        public string backColor { get; set; }
        [DataMember]
        public double backAmount { get; set; }
        [DataMember]
        public string foreColor { get; set; }
        [DataMember]
        public double foreAmount { get; set; }
        [DataMember]
        public string icon { get; set; }
        [DataMember]
        public bool useAsTransformation { get; set; }
        [DataMember]
        public bool hasAssetReadAccess { get; set; }
        [DataMember]
        public bool isSubjectInTransformation { get; set; }
        [DataMember]
        public AssetTypeClass @class { get; set; }
        [DataMember]
        public AssetBrowserApiHopDirection reveal { get; set; }
        [DataMember]
        public int actionCount { get; set; }
        [DataMember]
        public List<AssetBrowserOwnerCountModel> ownerCounts { get; set; }
        [DataMember]
        public List<AssetBrowserAssetRelationCountModel> relationCounts { get; set; }
        [DataMember]
        public List<AssetBrowserAssetModel> items { get; set; }
    }

    public class AssetBrowserAssetsModel
    {
        public List<AssetBrowserAssetModel> assets { get; set; } = new List<AssetBrowserAssetModel>();
        public List<AssetBrowserAssetRelationModel> assetRelations { get; set; } = new List<AssetBrowserAssetRelationModel>();
    }

    public class AssetBrowserAssetRelationCountModel
    {
        public string Predicate { get; set; }
        public int PredicateID { get; set; }
        public Guid PredicateUid { get; set; }
        public AssetBrowserApiHopDirection Direction { get; set; }
        public int Count { get; set; }
    }

    public class AssetBrowserAssetRelationModel
    {
        public Guid intersectUid { get; set; }
        public Guid subjectUid { get; set; }
        public string subjectKey { get; set; }
        public Guid objectUid { get; set; }
        public string objectKey { get; set; }
        public string predicate { get; set; }
        public int predicateId { get; set; }
        public Guid predicateUid { get; set; }
        public PredicateType predicateType { get; set; }
        public string backColor { get; set; }
        public string foreColor { get; set; }
        public string icon { get; set; }
    }

    #endregion

    #region Filter

    internal class AssetBrowserAssetTypeFilterItem
    {
        public Guid Uid { get; set; }
        public int AssetTypeId { get; set; }
        public int ClassId { get; set; }
        public string Class { get { return ((AssetTypeClass)ClassId).GetDisplayName(); } }
        public string Name { get; set; }
        public string Path { get; set; }
    }
    internal class AssetBrowserPredicateFilterItem
    {
        public int Id { get; set; }
        public Guid Uid { get; set; }
        public int TypeId { get; set; }
        public string Type { get { return ((PredicateType)TypeId).GetDisplayName(); } }
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
        public string AssetTypeClassDisplayName { get { return AssetTypeClass.GetDisplayName(); } }
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
        public string Name { get; set; }
        public int Value { get; set; }
    }

    internal class AssetBrowserDiagramAssetOwner
    {
        public int ResponsibilityTypeID { get; set; }
        public string ResponsibilityTypeName { get; set; }
        public string Icon { get; set; }
        public int ResourceID { get; set; }
        public string ResourceName { get; set; }
        public string SecurityAssetName { get; set; }
        public string Context { get; set; }
    }

    #endregion

    #region Hop

    internal class HopNodeResult
    {
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

        //[DataMember]
        public Guid assetUid { get; set; }

        [DataMember]
        public Guid resourceUid { get; set; }
    }

    public class AssetBrowserOwnersModel
    {
        public List<AssetBrowserOwnerModel> owners { get; set; } = new List<AssetBrowserOwnerModel>();
        public List<AssetBrowserAssetRelationModel> ownerRelations { get; set; } = new List<AssetBrowserAssetRelationModel>();
    }

    public class AssetBrowserOwnerCountModel
    {
        public string ResponsibilityType { get; set; }
        public int ResponsibilityTypeID { get; set; }
        public int Count { get; set; }
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