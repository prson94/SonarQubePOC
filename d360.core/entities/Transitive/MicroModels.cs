using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    public class AllocationPossibilityComparer : IEqualityComparer<AllocationPossibility>
    {
        public bool Equals(AllocationPossibility x, AllocationPossibility y)
        {
            return (x.ObjectType == y.ObjectType && x.ObjectTypeID == y.ObjectTypeID);
        }

        public int GetHashCode(AllocationPossibility obj)
        {
            return obj.ObjectType.GetHashCode() ^ obj.ObjectTypeID.GetHashCode();
        }
    }

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
        public string Name { get; set; }
    }

    public class CompanySsoModel
    {
        public bool AllowNewUserLogin { get; set; }
        public AuthenticationType AuthenticationType { get; set; }
        public byte[] IdpCertificateFile { get; set; }
        public string IdpCertificatePassword { get; set; }
        public string IdpSloEndpoint { get; set; }
        public string IdpSsoEndpoint { get; set; }
        public byte[] SpCertificateFile { get; set; }
        public string SpCertificatePassword { get; set; }
        public HashAlgorithmType HashAlgorithmType { get; set; }

        public bool SignInitialSSORequest { get; set; }
    }

    /// <summary>
    /// Used in CompanyConnectionUtils.
    /// </summary>
    public class CompanyWithDatabaseServerSettings
    {
        public int CompanyID { get; set; }
        public int ClientID { get; set; }
        public string Status { get; set; }
        public string Server { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string FusionQueue { get; set; }
        public string SearchServer { get; set; }
        public string EventTopic { get; set; }
        public bool IsDevelopment { get; set; }
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
        public int ID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class LoadDetail: BaseObject
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

    public class TopNavigationItem
    {
        public string MenuID { get; set; }
        public Feature Feature { get; set; }
        public bool ShouldDisplay { get; set; }
        public string Items { get; set; }
        public List<NavigationItem> NavigationItems { get; set; }
        public int SortOrder { get; set; }
        public string Icon { get; set; }
        public string Title { get; set; }

    }

    public class NavigationItem
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string MenuID { get; set; }
        public List<NavigationItem> Items { get; set; }
    }

    public class AddSiteNavModel
    {
        public SiteNav Folder { get; set; }
        public List<SiteNav> Items { get; set; } = new List<SiteNav>();
    }

    [DataContract(Namespace = NAMESPACE)]
    public class ObjectModel: BaseObject
    {
        [DataMember]
        public string ObjectType { get; set; }
        [DataMember]
        public int ObjectID { get; set; }
    }

    #region ResponsibilityRule Models

    public class ObjectResult
    {
        public long AssetID { get; set; }
        public string Name { get; set; }
    }

    public class SecurityResult
    {
        public string SecurityAsset { get; set; }
        public int SecurityAssetID { get; set; }
        public string Name { get; set; }
    }

    public class EndTypeResult
    {
        public int RuleID { get; set; }

        public int ResponsibilityTypeID { get; set; }

        public string SecurityAsset { get; set; }

        public int SecurityAssetID { get; set; }
    }

    public class EndResult: EndTypeResult
    {
        public long AssetID { get; set; }
    }

    #endregion

    [DataContract(Namespace = NAMESPACE)]
    public class PermissionModel : BaseObject
    {
        [DataMember]
        public string Claim { get; set; }

        [DataMember]
        public string ClaimObject { get; set; }
    }

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

    [DataContract(Namespace = NAMESPACE)]
    public class ReportSchemaModel: BaseObject
    {
        [DataMember]
        public string ID { get; set; }

        [DataMember]
        public string ParentID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Schema { get; set; }

        [DataMember]
        public int Position { get; set; }

        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public List<ReportSchemaModel> Items { get; set; }
    }

    public class BulkLoadRelationModel
    {
        public string DisplayValue { get; set; }

        public string Object { get; set; }

        public int ObjectID { get; set; }
    }

    public class BulkLoadMatchingModel
    {
        public int FieldTypeID { get; set; }
        public int ColumnIndex { get; set; }
        public List<BulkLoadMatchingFieldModel> Fields { get; set; }
    }

    public class BulkLoadMatchingFieldModel
    {        
        public string Value { get; set; }
        public int ObjectID { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class FusionStatisticTileModel : BaseObject
    {
        [DataMember]
        public int AgentErrors { get; set; }

        [DataMember]
        public int AgentExecutions { get; set; }

        [DataMember]
        public int FusionExecutions { get; set; }

        [DataMember]
        public int FusionErrors { get; set; }

        [DataMember]
        public int PromotionJobsExecuted { get; set; }
    }

    public class FusionAddItemModel
    {
        public int RuleID { get; set; }
        public bool AllSelected { get; set; }
        public string attributeIDs { get; set; }
        public string ObjectType { get; set; }
    }
}
