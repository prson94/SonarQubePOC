using d360.core.enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    public class BasicAsset
    {
        public int AssetID { get; set; }
        public int ObjectID { get; set; }
        public string ObjectName { get; set; }
    }

    public class AllocationPossibility
    {
        public int ObjectTypeID { get; set; }
        public string ObjectType { get; set; }
        public AssetTypeClass Class { get; set; }
        public string ClassName { get { return Class.GetDisplayName(); } }
        public string Name { get; set; }
    }

    /// <summary>
    /// Used in CompanyConnectionUtils.
    /// </summary>
    public class CompanyWithDatabaseServerSettings
    {
        public int CompanyID { get; set; }
        public int ClientID { get; set; }
        public string Server { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string SearchServer { get; set; }
        public string EventTopic { get; set; }
        public string UrlPrefix { get; set; }
        public EnvironmentLevel EnvironmentLevel { get; set; }
        public int Priority { get; set; }
    }

    public class FieldsObjectModel
    {
        public SystemObjects Type { get; set; }
        public int TypeID { get; set; }

        public SystemObjects @Object { get; set; }
    }

    public class GetUserModel
    {
        [JsonProperty("uid")]
        public Guid Uid { get; set; }

        [JsonProperty("fullName")]
        public string FullName { get; set; }
    }

    [DataContract(Namespace = NAMESPACE), System.ComponentModel.DataAnnotations.Schema.Table("Global_Resource", Schema = "reporting")]
    public class GlobalReportingResource : BaseObject
    {
        [DataMember, Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ResourceID { get; set; }

        [DataMember]
        public string FirstName { get; set; }

        [DataMember]
        public string LastName { get; set; }

        [DataMember]
        public DateTime? LastLoggedInOn { get; set; }

        [DataMember]
        public string Email { get; set; }

        [DataMember]
        public CompanyResourceState State { get; set; }

        [DataMember]
        public bool IsAdministrator { get; set; }

        [DataMember]
        public DateTime? CreatedOn { get; set; }

        [DataMember]
        public DateTime? UpdatedOn { get; set; }

        [DataMember]
        public Guid Uid { get; set; }

        [DataMember, NotMapped]
        public string FullName { get { return FirstName + " " + LastName; } }


        #region Deprecated

        [NotMapped, DataMember]
        public DateTime? DateLastLoggedIn { get; set; }

        [NotMapped, DataMember]
        public string Status { get; set; }

        #endregion
    }

    public partial class IntersectTypeOption
    {
        public Guid Uid { get; set; }
        public int ID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class LoadDetail : BaseObject
    {
        [DataMember]
        public int ID { get; set; }
        [DataMember]
        public string Object { get; set; }
        [DataMember]
        public int ObjectID { get; set; }
        [DataMember]
        public string ObjectName { get; set; }
        [DataMember]
        public string Notes { get; set; }
        [DataMember]
        public string ErrorMessage { get; set; }
        [DataMember]
        public string FilePath { get; set; }
        [DataMember]
        public DateTime? DateStarted { get; set; }
        [DataMember]
        public DateTime? DateCompleted { get; set; }
        [DataMember]
        public string Action { get; set; }
        [DataMember]
        public int Success { get; set; }
        [DataMember]
        public int Error { get; set; }
        [DataMember]
        public int Incomplete { get; set; }
        [DataMember]
        public int Total { get; set; }
        [DataMember]
        public string Requestor { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class LoadDetailV2 : BaseObject
    {
        [DataMember]
        public string Action { get; set; }
        [DataMember]
        public DateTime? DateCompleted { get; set; }
        [DataMember]
        public DateTime? DateStarted { get; set; }
        [DataMember]
        public Guid AssetTypeUid { get; set; }
        [DataMember]
        public string AssetTypeName { get; set; }
        [DataMember]
        public string RequestedByName { get; set; }
        [DataMember]
        public Guid RequestedByUid { get; set; }
        [DataMember]
        public int Total { get; set; }
        [DataMember]
        public string ErrorMessage { get; set; }
        [DataMember]
        public Guid LoadUid { get; set; }
    }



    public class TopNavigationItem
    {
        public string MenuID { get; set; }
        public bool ShowVisibilityToggle { get; set; }
        public bool ShouldDisplay { get; set; } = true;
        [JsonIgnore]
        public string Items { get; set; }
        public List<NavigationItem> NavigationItems { get; set; }
        public int SortOrder { get; set; }
        public string Icon { get; set; }
        public string ImageIconUrl { get; set; }
        public string FullURL
        {
            get
            {
                if (string.IsNullOrEmpty(this.ImageIconUrl))
                {
                    return null;
                }
                else
                {
                    return constants.COMPANY_RESOURCES_URL + this.ImageIconUrl;
                }
            }
        }
        public string Title { get; set; }

    }

    public class NavigationItem
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string MenuID { get; set; }
        public bool ShowChildren { get; set; }
        public List<NavigationItem> Items { get; set; }
    }

    public class AddSiteNavModel
    {
        public SiteNav Folder { get; set; }
        public List<SiteNav> Items { get; set; } = new List<SiteNav>();
    }

    #region ResponsibilityRule Models

    public class ObjectResult
    {
        public Guid? uid { get; set; }
        public long AssetID { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
    }

    public class SecurityResult
    {
        public Guid uid { get; set; }
        public string SecurityAsset { get; set; }
        public int SecurityAssetID { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
    }

    #endregion

    [DataContract(Namespace = NAMESPACE)]
    public class ObjectStatisticTileModel : BaseObject
    {
        [DataMember]
        public int FollowerCount { get; set; }

        [DataMember]
        public int CommentCount { get; set; }

        [DataMember]
        public DateTime? CommentLast { get; set; }

        [DataMember]
        public int? Score { get; set; }

        [DataMember]
        public DateTime? ScoreLast { get; set; }

        [DataMember]
        public int IssueCount { get; set; }

        [DataMember]
        public DateTime? IssueLast { get; set; }

        [DataMember]
        public List<ObjectStatisticTileItemModel> Items { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class ObjectStatisticTileItemModel : BaseObject
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Url { get; set; }

        [DataMember]
        public int Count { get; set; }

        [DataMember]
        public int TypeID { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class RawObjectStatistic : BaseObject
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int? Value { get; set; }

        [DataMember]
        public string Group { get; set; }

        [DataMember]
        public string Url { get; set; }

        [DataMember]
        public DateTime? MostRecent { get; set; }

        [DataMember]
        public int TypeID { get; set; }
    }

    public class RelationshipDirectionFieldInfo
    {
        public bool IsSubject { get; set; }
        public int FieldTypeID { get; set; }
        public int IntersectTypeID { get; set; }
        public string Object { get; set; }
        public int ObjectID { get; set; }
    }

    public class TypeIdentifierInfoModel
    {
        public int? ID { get; set; }

        public string Object { get; set; }
        public int ObjectID { get; set; }

        public Guid Uid { get; set; }
    }

    public class SecondaryNavigationPostModel
    {
        public int? ObjectId { get; set; }
        public string ObjectType { get; set; }
        public int? AssetId { get; set; }
        public Guid? AssetUid { get; set; }
        public Guid? AssetTypeUid { get; set; }
        public bool PreloadData { get; set; }
        public AssetTypeClass Class { get; set; }
    }

    public class SecondaryNavigationResponseModel
    {
        public int AssetId { get; set; }
        public int AssetTypeId { get; set; }
        public Guid Uid { get; set; }
        public string Object { get; set; }
        public string ObjectType { get; set; }
        public int ObjectTypeId { get; set; }
        public int ObjectID { get; set; }
        public string DisplayValue { get; set; }
        public string MainTabTitle { get; set; }
        public string TypeName { get; set; }
        public SecondaryNavItems Items { get; set; }
        public JObject Artifact { get; set; }
        public dynamic PreloadData { get; set; }
    }

    public class SecondaryNavItems
    {
        public bool HasAudit { get; set; }
        public bool HasOwnership { get; set; }
        public bool HasDashboard { get; set; }
        public bool HasLineage { get; set; }
        public bool HasImpact { get; set; }
        public bool HasRelationship { get; set; }
        public bool HasFollowers { get; set; }
        public bool HasWorkflow { get; set; }
        public bool HasField { get; set; }
        public bool HasChild { get; set; }
        public bool HasRuleResult { get; set; }
        public bool HasGovernanceRoleUidSet { get; set; }
        public bool HasProcessDiagram { get; set; }
        public bool HasRequestCertificationWorkflow { get; set; }
        public bool HasGroups { get; set; }
        public bool HasFollowing { get; set; }
        public bool HasItemOwn { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class LoadItemDetail : BaseObject
    {
        [DataMember]
        public int RowIndex { get; set; }
        [DataMember]
        public string Column1 { get; set; }
        [DataMember]
        public string Column2 { get; set; }
        [DataMember]
        public string Column3 { get; set; }
        [DataMember]
        public string Column4 { get; set; }
        [DataMember]
        public string Column5 { get; set; }
        [DataMember]
        public string Column6 { get; set; }
        [DataMember]
        public string Status { get; set; }
        [DataMember]
        public string StatusMessage { get; set; }

    }

    [DataContract(Namespace = NAMESPACE)]
    public class SingleLoadDetail : BaseObject
    {
        [DataMember]
        public int Total { get; set; }
        [DataMember]
        public int Success { get; set; }
        [DataMember]
        public int Error { get; set; }
        [DataMember]
        public int Incomplete { get; set; }
        [DataMember]
        public string Action { get; set; }
        [DataMember]
        public string AssetTypeName { get; set; }
        [DataMember]
        public Guid AssetTypeUid { get; set; }
        [DataMember]
        public string ElapsedTime { get; set; }
        [DataMember]
        public string Status { get; set; }
        [DataMember]
        public string RequestedByName { get; set; }
        [DataMember]
        public Guid RequestedByUid { get; set; }
    }

    public class LevelField
    {
        public int Level { get; set; }
        public string Name { get; set; }
        public bool PartOfKey { get; set; }
        public bool Required { get; set; }
        public int ColumnIndex { get; set; }
        public bool DataLoaded { get; set; }
    }

    public class LoadLevelStatus
    {
        public int Level { get; set; }
        public bool Required { get; set; }
        public bool DataLoaded { get; set; }
    }

    public class LoadLevelStatusComparer : IEqualityComparer<LoadLevelStatus>
    {
        public bool Equals(LoadLevelStatus x, LoadLevelStatus y)
        {
            return (x.Level == y.Level);
        }

        public int GetHashCode(LoadLevelStatus obj)
        {
            return obj.Level.GetHashCode();
        }
    }

}