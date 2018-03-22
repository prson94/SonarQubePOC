using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace igx.jobs.igc
{
    #region Generic

    public class GenericIgcContextModel
    {
        public string _type { get; set; }
        public string _id { get; set; }
        public string _url { get; set; }
        public string _name { get; set; }
    }

    public class GenericIgcPagingModel
    {
        public int numTotal { get; set; }
        public string next { get; set; }
        public int pageSize { get; set; }
        public int end { get; set; }
        public int begin { get; set; }
    }

    public class IgcModel
    {
        [JsonProperty(PropertyName = "_id")]
        public string SourceID { get; set; }

        [JsonProperty(PropertyName = "_name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "short_description")]
        public string ShortDescription { get; set; }

        [JsonProperty(PropertyName = "_url")]
        public string IgcUrl { get; set; }

        [JsonProperty(PropertyName = "_type")]
        public string Type { get; set; }

        public List<GenericIgcContextModel> _context { get; set; }
    }

    public class IgcModels
    {
        public GenericIgcPagingModel paging { get; set; }
    }

    public class IgcDynamicModels : IgcModels
    {
        public List<dynamic> items { get; set; }
    }

    public class IgcDynamicArrayModels : IgcModels
    {
        public JArray items { get; set; }
    }

    #endregion

    public class MappingType
    {
        public int Id { get; set; }
        public string IgcType { get; set; }
        public string GovernType { get; set; }
        public int GovernTypeID { get; set; }
        public bool ToGovern { get; set; } = true;
        public bool Active { get; set; } = true;
    }

    public class MappingItem
    {
        public int MappingTypeId { get; set; }
    }

    public class MappingFieldItem: MappingItem
    {
        public string IgcField { get; set; }
        public string GovernField { get; set; }
        public int? ParentContextPosition { get; set; } = null; // If this is populated, then we need to grab the value of the _id from the _context collection, based on the position.
        public bool IsArray { get; set; } = false;
    }

    public class MappingRelationItem : MappingItem
    {
        public string IgcField { get; set; }
        public int GovernPredicateType { get; set; }
        public bool IsSubject { get; set; } = false;
    }

    public class MappingRoleItem : MappingItem
    {
        public string IgcIdField { get; set; } = string.Empty;
        public string IgcNameField { get; set; } = string.Empty;
        public string GovernRoleName { get; set; }
    }
    

    #region Specific

    #region ApplicationCatalog

    public class IgcApplicationCatalogModels : IgcModels
    {
        public List<IgcApplicationCatalogModel> items { get; set; }
    }

    public class IgcApplicationCatalogModel : IgcModel
    {
        [JsonProperty(PropertyName = "$MaturityLevel")]
        public string MaturityLevel { get; set; }

        [JsonProperty(PropertyName = "$CMDBAppCode")]
        public string CMDBAppCode { get; set; }

        [JsonProperty(PropertyName = "$PersonalData")]
        public string PersonalData { get; set; }

        [JsonProperty(PropertyName = "$ComponentType")]
        public string ComponentType { get; set; }

        [JsonProperty(PropertyName = "$DataOwner")]
        public string DataOwner { get; set; }

        [JsonProperty(PropertyName = "$DataSteward")]
        public string DataSteward { get; set; }

        [JsonProperty("$KeyApplicationType")]
        public string[] KeyApplicationType { get; set; }

        [JsonProperty(PropertyName = "$AuthoritativeSource")]
        public string AuthoritativeSource { get; set; }

        [JsonProperty(PropertyName = "$BusinessOwnerId")]
        public string BusinessOwnerId { get; set; }

        [JsonProperty(PropertyName = "$ComponentSAID")]
        public string ComponentSAID { get; set; }

        [JsonProperty(PropertyName = "$BookOfRecord")]
        public string BookOfRecord { get; set; }

        [JsonProperty(PropertyName = "$DataLocation")]
        public string DataLocation { get; set; }

        [JsonProperty(PropertyName = "$Comments")]
        public string Comments { get; set; }

        [JsonProperty(PropertyName = "$ApplicationAlias")]
        public string ApplicationAlias { get; set; }

        [JsonProperty(PropertyName = "long_description")]
        public string LongDescription { get; set; }

        [JsonProperty(PropertyName = "$SSID")]
        public string SSID { get; set; }

        [JsonProperty(PropertyName = "$ApplicationOwnerId")]
        public string ApplicationOwnerId { get; set; }

        [JsonProperty(PropertyName = "$ApplicationOwner")]
        public string ApplicationOwner { get; set; }

        [JsonProperty(PropertyName = "$Status")]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "$DataStewardId")]
        public string DataStewardId { get; set; }

        [JsonProperty(PropertyName = "$EDGMStewardId")]
        public string EDGMStewardId { get; set; }

        [JsonProperty(PropertyName = "$BusinessOwner")]
        public string BusinessOwner { get; set; }

        [JsonProperty(PropertyName = "impacts_on")]
        public IgcRelationshipModel ImpactsOn { get; set; }
    }

    public class D3sApplicationCatalogModel
    {
        public string SourceID { get; set; }

        public string Name { get; set; }

        public string ShortDescription { get; set; }

        public string MaturityLevel { get; set; }

        public string CMDBAppCode { get; set; }

        public string PersonalData { get; set; }

        public string ComponentType { get; set; }

        public string KeyApplicationTypeText { get; set; }

        public string AuthoritativeSource { get; set; }

        public string ComponentSAID { get; set; }

        public string Host { get; set; }

        public string DataLocation { get; set; }

        public string Comments { get; set; }

        public string ApplicationAlias { get; set; }

        public string LongDescription { get; set; }

        public string SSID { get; set; }

        public string Status { get; set; }
    }

    public class D3sOwnershipItemsModel
    {
        public string UserIdFieldName { get; set; }
        public List<D3sOwnershipModel> Items { get; set; }
    }

    public class D3sOwnershipModel
    {
        public string SourceID { get; set; }
        public string RoleName { get; set; }
        public string UserId { get; set; }

        [JsonIgnore]
        public string UserFullName { get; set; }
    }

    #endregion

    #region Data File

    public class IgcDataFileModels : IgcModels
    {
        public List<IgcDataFileModel> items { get; set; }
    }

    public class IgcDataFileModel : IgcModel
    {
        [JsonProperty(PropertyName = "parent_folder")]
        public string ParentFolder { get; set; }

        [JsonProperty(PropertyName = "custom_Status")]
        public string CustomStatus { get; set; }

        [JsonProperty(PropertyName = "custom_Data Steward")]
        public string DataSteward { get; set; }

        [JsonProperty(PropertyName = "custom_Data Steward Id")]
        public string DataStewardId { get; set; }

        [JsonProperty(PropertyName = "custom_Comments")]
        public string Comments { get; set; }

        [JsonProperty(PropertyName = "custom_Classification")]
        public string Classification { get; set; }

        [JsonProperty(PropertyName = "location")]
        public string Location { get; set; }

        [JsonProperty(PropertyName = "long_description")]
        public string LongDescription { get; set; }

        [JsonProperty(PropertyName = "notes")]
        public string Notes { get; set; }

        [JsonProperty(PropertyName = "impacts_on")]
        public IgcRelationshipModel ImpactsOn { get; set; }

        [JsonProperty(PropertyName = "impacted_by")]
        public IgcRelationshipModel ImpactedBy { get; set; }
    }

    public class D3sDataFileModel
    {
        public string SourceID { get; set; }

        public string Name { get; set; }

        public string ShortDescription { get; set; }

        public string Host { get; set; }

        public string DataLocation { get; set; }

        public string Comments { get; set; }

        public string LongDescription { get; set; }
    }

    #endregion

    #region RRP

    #region RRP Functional Area

    public class IgcRrpFunctionalAreaModels : IgcModels
    {
        public List<IgcRrpFunctionalAreaModel> items { get; set; }
    }


    public class IgcRrpFunctionalAreaModel : IgcModel
    {
        [JsonProperty(PropertyName = "long_description")]
        public string LongDescription { get; set; }
    }

    public class D3sRrpFunctionalAreaModel
    {
        public string SourceID { get; set; }

        public string Name { get; set; }

        public string ShortDescription { get; set; }

        public string LongDescription { get; set; }
    }

    #endregion

    #region RRP Level One

    public class IgcRrpLevelOneModels : IgcModels
    {
        public List<IgcRrpLevelOneModel> items { get; set; }
    }


    public class IgcRrpLevelOneModel : IgcModel
    {
        [JsonProperty(PropertyName = "long_description")]
        public string LongDescription { get; set; }
    }

    public class D3sRrpLevelOneModel
    {
        public string SourceID { get; set; }

        public string ParentSourceID { get; set; }

        public string Name { get; set; }

        public string ShortDescription { get; set; }

        public string LongDescription { get; set; }
    }

    #endregion

    #region RRP Level Two

    public class IgcRrpLevelTwoModels : IgcModels
    {
        public List<IgcRrpLevelTwoModel> items { get; set; }
    }


    public class IgcRrpLevelTwoModel : IgcModel
    {
        [JsonProperty(PropertyName = "long_description")]
        public string LongDescription { get; set; }
    }

    public class D3sRrpLevelTwoModel
    {
        public string SourceID { get; set; }

        public string ParentSourceID { get; set; }

        public string Name { get; set; }

        public string ShortDescription { get; set; }

        public string LongDescription { get; set; }
    }

    #endregion

    #region RRP Level Three

    public class IgcRrpLevelThreeModels : IgcModels
    {
        public List<IgcRrpLevelThreeModel> items { get; set; }
    }


    public class IgcRrpLevelThreeModel : IgcModel
    {
        [JsonProperty(PropertyName = "long_description")]
        public string LongDescription { get; set; }
    }

    public class D3sRrpLevelThreeModel
    {
        public string SourceID { get; set; }

        public string ParentSourceID { get; set; }

        public string Name { get; set; }

        public string ShortDescription { get; set; }

        public string LongDescription { get; set; }
    }

    #endregion

    #endregion

    #region BU

    #region Business Unit Top

    public class IgcBuTopModels : IgcModels
    {
        public List<IgcBuTopModel> items { get; set; }
    }

    public class IgcBuTopModel : IgcModel
    {
        [JsonProperty(PropertyName = "long_description")]
        public string LongDescription { get; set; }

        [JsonProperty(PropertyName = "$BusinessUnitId")]
        public string BusinessUnitId { get; set; }

        [JsonProperty(PropertyName = "$BusinessOwnerId")]
        public string BusinessOwnerId { get; set; }

        [JsonProperty(PropertyName = "$BusinessOwner")]
        public string BusinessOwner { get; set; }
    }

    public class D3sBuTopModel
    {
        public string SourceID { get; set; }

        public string Name { get; set; }

        public string ShortDescription { get; set; }

        public string LongDescription { get; set; }

        public string BusinessUnitID { get; set; }

        public string BusinessOwnerId { get; set; }

        public string BusinessOwner { get; set; }
    }

    #endregion

    #region Business Unit Child

    public class IgcBuChildModels : IgcModels
    {
        public List<IgcBuChildModel> items { get; set; }
    }

    public class IgcBuChildModel : IgcModel
    {
        [JsonProperty(PropertyName = "long_description")]
        public string LongDescription { get; set; }

        [JsonProperty(PropertyName = "$BusinessUnitId")]
        public string BusinessUnitId { get; set; }

        [JsonProperty(PropertyName = "$BusinessOwnerId")]
        public string BusinessOwnerId { get; set; }

        [JsonProperty(PropertyName = "$BusinessOwner")]
        public string BusinessOwner { get; set; }
    }

    public class D3sBuChildModel
    {
        public string SourceID { get; set; }

        public string ParentSourceID { get; set; }

        public string Name { get; set; }

        public string ShortDescription { get; set; }

        public string LongDescription { get; set; }

        public string BusinessUnitID { get; set; }

        public string BusinessOwnerId { get; set; }

        public string BusinessOwner { get; set; }
    }

    #endregion

    #endregion

    #region Relationships

    public class IgcRelationshipModel : IgcModels
    {
        public List<IgcModel> items { get; set; }
    }

    public class IgcBusinesUnitApplicationCatalogRelationshipModels : IgcModels
    {
        public List<IgcBusinesUnitApplicationCatalogRelationshipModel> items { get; set; }
    }


    public class IgcBusinesUnitApplicationCatalogRelationshipModel : IgcModel
    {
        [JsonProperty(PropertyName = "impacts_on")]
        public IgcRelationshipModel ImpactsOn { get; set; }
    }

    public class D3sRelationshipModel
    {
        public string SubjectSourceID { get; set; }

        public string ObjectSourceID { get; set; }

        public int PredicateType { get; set; }
    }

    #endregion

    #endregion
}
