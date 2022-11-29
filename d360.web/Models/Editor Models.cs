using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Web.Mvc;

using d360.core;
using d360.core.entities;
using d360.core.enums;

using Newtonsoft.Json;

namespace d360.web.Models
{
    public abstract class BaseEditorModel
    {
        public string FormUri { get; set; }

        public string FormMethod { get; set; }

        public string FormName { get; set; }

        public string FormDescription { get; set; }

        public bool IsUsed { get; set; }
    }

    public class PrimeSelectItem
    {
        public string label { get; set; }

        public string value { get; set; }
    }

    public class InsertUserToGroup
    {
        public Guid Uid { get; set; }
    }

    public class UpdateCss
    {
        public string css { get; set; }
    }

    public class AssetTypeEditorModel : BaseEditorModel
    {
        public AssetTypeUpsert AssetType { get; set; }

        public int? ParentID { get; set; } = null;

        public Guid? ParentUid { get; set; } = null;

        public List<PrimeSelectItem> Predicates { get; set; } = new List<PrimeSelectItem>();

        public List<PrimeSelectItem> Tokens { get; set; } = new List<PrimeSelectItem>();

        public List<PrimeSelectItem> Parents { get; set; } = new List<PrimeSelectItem>();
    }

    public class KnockoutDisplayItem
    {
        public string title { get; set; }

        public string value { get; set; }
    }

    public class NymAllocationModel
    {
        public SystemObjects Object { get; set; }

        public int ObjectID { get; set; }

        public int[] PredicateIDs { get; set; }
    }

    public class CompanyRebuildJobRequest
    {
        public CompanyRebuildJobToken Job { get; set; }
    }

	public class UserLanguageModel
	{
		public string LanguageCode { get; set; }
	}

	public class CompanySettingsIpRestrictionEditorModel
    {
        public string Name { get; set; }

        public string Start { get; set; }

        public string End { get; set; }
    }

    public class CompanySettingsEditorModel
    {
        public CompanySettingsEditorModel()
        {
            IpRestrictions = new List<CompanySettingsIpRestrictionEditorModel>();
            SetIconToDefault = false;
            SetLogoToDefault = false;
        }

        public bool DisableCommunityPosting { get; set; }

        public bool DisableIssueManagement { get; set; }

        public string CompanyLogo { get; set; }

        public bool SetLogoToDefault { get; set; }

        public string CurrentCompanyLogoPath { get; set; }

        public string CompanyIcon { get; set; }

        public bool SetIconToDefault { get; set; }

        public string CurrentCompanyIconPath { get; set; }

        public bool EnableShoppingCart { get; set; }

        public string DefaultRoute { get; set; }

        public bool EnableSagacity { get; set; }

        public List<CompanySettingsIpRestrictionEditorModel> IpRestrictions { get; set; }

        public List<SiteNav> SiteNav { get; set; } = new List<SiteNav>();

        public string DefaultSearchTypes { get; set; }

        public bool HideData3SixtyUsers { get; set; }

        public bool ShowAllUsersAPIKey { get; set; }

        public int WorkflowCatchAllGroup { get; set; }

        public bool ShowHomeAssignmentTile { get; set; }

        public bool ShowHomeBoardTile { get; set; }

        public bool ShowHomeActivityTile { get; set; }

        public bool ShowHomePageTitle { get; set; }

        public string HomePageTitleSize { get; set; }

        public string HomePageTitleColor { get; set; }

        public string HomePageBackgroundImage { get; set; }

        public bool ClearHomePageBackgroundImage { get; set; } = false;

        public string BrowserTitlePrefix { get; set; }

        public int WorkflowDigestEmailDays { get; set; }

        public int MaxDropdownItems { get; set; }

        public bool WriteActionDescription { get; set; }

        public int LineageVersion { get; set; } = 1;

        public int MaxExcelExportRows { get; set; }

        public string AllowedOrigins { get; set; }

        public string FramingDomains { get; set; }

        public bool HideHeaderBarControls { get; set; }

        public int AssetDefinitionColumnWidth { get; set; }

        public int DiagramMaxAvoidNodesLinkCount { get; set; }

        public string RequestCertificationDraft { get; set; }
    }

    public class DataQualityResult
    {
        public int PassCount { get; set; }

        public int FailCount { get; set; }

        public DateTime EffectiveDate { get; set; }

        public DateTime RunDate { get; set; }

        public int ID { get; set; }
    }

    public class DataQualityResultItem
    {
        public DataQualityResult Result { get; set; }

        public List<DataQualityAssetMapping> AssetsMappings { get; set; }
    }

    public class DataQualityAssetMapping
    {
        public string AssetPath { get; set; }

        public Guid? AssetUID { get; set; }
    }

    public class DataQualityResultModel
    {
        public List<DataQualityResultItem> Results { get; set; }

        public int? Timeout { get; set; }
    }

    [DataContract(Namespace = constants.NAMESPACE)]
    public class EditableField : ReadOnlyField
    {
        public EditableField()
        {
            Items = new List<SelectListItem>();
            ReadOnly = false;
            MultiSelect = false;
            UseTypeahead = false;
        }

        [DataMember]
        public string DataUri { get; set; }

        [DataMember]
        public string FieldType { get; set; }

        [DataMember]
        public List<SelectListItem> Items { get; set; }

        [DataMember]
        public bool ReadOnly { get; set; }

        [DataMember]
        public string TooltipText { get; set; }

        [DataMember]
        public bool Required { get; set; }

        [DataMember]
        public bool MultiSelect { get; set; }

        [DataMember]
        public int? RangeMin { get; set; }

        [DataMember]
        public int? RangeMax { get; set; }

        [DataMember]
        public List<FieldValidationModel> Validations { get; set; }

        [DataMember]
        public string TypeaheadUri { get; set; }

        [DataMember]
        public string SimilarItemsUri { get; set; }
        [DataMember]
        public string Category { get; set; }

        [DataMember]
        public int? ParentFieldTypeID { get; set; }

        [DataMember]
        public string ParentFieldTypeName { get; set; }

        [DataMember]
        public int FieldTypeID { get; set; }

        [DataMember]
        public int RecordCount { get; set; }

        [DataMember]
        public bool UseTypeahead { get; set; }

        [DataMember]
        public string DelayedLoadType { get; set; }

        [DataMember]
        public bool IsSemantic { get; set; }

        [DataMember]
        public bool VirtualScroll { get; set; }

        [DataMember]
        public int? ItemSize { get; set; }

        [DataMember]
        public bool UseNativeLookupControl => !(VirtualScroll || UseTypeahead);

        [DataMember]
        public bool UseColorControl { get; set; }

        [DataMember]
        public bool IsAssetLazyLoad { get; set; }

        [DataMember]
        public Guid AssetUid { get; set; }

        [DataMember]
        public Guid IntersectTypeUid { get; set; }

        [DataMember]
        public Guid TargetAssetTypeUid { get; set; }

        [DataMember]
        public Cardinality ObjectCardinality { get; set; }
    }

    [DataContract(Namespace = constants.NAMESPACE)]
    public class EditableFieldItem
    {
        public EditableFieldItem()
        {
            Selected = false;
        }

        [DataMember]
        public string Text { get; set; }

        [DataMember]
        public string Value { get; set; }

        [DataMember]
        public string Group { get; set; }

        [DataMember]
        public string Group2 { get; set; }

        [DataMember]
        public bool Selected { get; set; }
    }

    public class FieldTypeItemDisplayFieldEditorModel
    {
        public int FieldTypeID { get; set; }

        public string FieldTypeName { get; set; }

        public bool Show { get; set; }

        public int? SortOrder { get; set; }

        public string FilterValue { get; set; }

        public bool Filter { get; set; }

        public string Object { get; set; }

        public int ObjectID { get; set; }

        public string OverrideDisplayName { get; set; }

        public int DisplayOrder { get; set; }

        public int? Width { get; set; }
    }

    public class FieldValidity
    {
        public FieldValidity()
        {
            Valid = true;
        }

        public bool Valid { get; set; }

        public string Message { get; set; }
    }

    public class FieldTypeRelationItemEditorModel
    {
        public int ID { get; set; }

        public int IntersectType { get; set; }

        public int ReferenceType { get; set; }

        public int? ChildIntersectType { get; set; }

        public int Direction { get; set; } = 0;

        public ICollection<FieldTypeItemDisplayFieldEditorModel> DisplayFields { get; set; }

        public bool HideHeader { get; set; }

        public bool HideFooter { get; set; }

        public bool HideFilter { get; set; } = false;

        public string Object { get; set; }

        public int ObjectID { get; set; }

        public int RelationType { get; set; }

        public FieldValidity Validation()
        {
            var prefix = "You are missing a";
            var valid = new FieldValidity();

            if (IntersectType <= 0)
            {
                valid.Valid = false;
                valid.Message = $"{prefix} relation.";
            }
            else
            {
                if (ReferenceType <= 0)
                {
                    valid.Valid = false;
                    valid.Message = $"{prefix} reference type.";
                }
                else
                {
                    if (ReferenceType > 1 && !ChildIntersectType.HasValue)
                    {
                        valid.Valid = false;
                        valid.Message = $"{prefix} child relation.";
                    }
                }
            }

            if (valid.Valid)
            {
                if (DisplayFields == null)
                {
                    valid.Valid = false;
                    valid.Message = $"{prefix} reference column.";
                }
                else
                {
                    if (DisplayFields.Count == 0)
                    {
                        valid.Valid = false;
                        valid.Message = $"{prefix} reference column.";
                    }
                }
            }

            return valid;
        }
    }

    public class FieldTypeOwnershipLookupEditorModel
    {
        public bool HideHeader { get; set; }

        public bool HideFooter { get; set; }

        public bool HideFilter { get; set; } = false;

        public string Object { get; set; }

        public int ObjectID { get; set; }

        public bool DisplayAssignmentSource { get; set; }

        public bool ExpandGroupMembership { get; set; }

        public FieldValidity Validation()
        {
            var valid = new FieldValidity();

            return valid;
        }
    }

    public class FieldTypeJsoneElementEditorModel
    {
        public int FieldTypeID { get; set; }

        public string Path { get; set; }

        public string DataType { get; set; }

        public FieldValidity Validation()
        {
            var valid = new FieldValidity();
            return valid;
        }
    }

    public class FieldTypeEditorModel
    {
        public bool FieldIsUsed { get; set; }

        public FieldType FieldType { get; set; }

        public FieldTypeRelationItemEditorModel RelationItem { get; set; }

        public List<FieldTypeRelationItemEditorModel> RelationItems { get; set; }

        public FieldTypeOwnershipLookupEditorModel OwnershipLookupSettings { get; set; }

        public FieldTypeJsoneElementEditorModel JsonElementSettings { get; set; }

        public FieldValidity Validation()
        {
            var valid = new FieldValidity();

            return valid;
        }
    }

    [DataContract(Namespace = constants.NAMESPACE)]
    public class FieldValidationModel
    {
        /// <summary>
        /// The error message to display to the user.
        /// </summary>
        public string message { get; set; }

        /// <summary>
        /// required; length=3,12; right:0,0; phone; ssn; zipCode; email; inline javascript function
        /// </summary>
        public string rule { get; set; }

        public string regex { get; set; }
    }

    public class LoginModel
    {
        [Required]
        [Display(Name = "User name")]
        public string UserName { get; set; }

        [Required]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    #region Asset Browser

    public enum AssetBrowserApiHopDirection
    {
        None = 0,
        Forward = 1,
        Backward = 2,
        Both = 3
    }

    public enum AssetBrowserDiagramType
    {
        Lineage = 1,
        Impact = 2,
        Process = 3
    }

    [DataContract]
    public class AssetBrowserApiOwnerHopRequestModel
    {
        [DataMember]
        public List<AssetBrowserApiHopAssetRequestModel> assets { get; set; }

        [DataMember]
        public string hierarchyKey { get; set; }

        [DataMember]
        public int responsibilityTypeId { get; set; }
    }

    [DataContract]
    public class AssetBrowserApiHopAssetRequestModel
    {
        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public string Key { get; set; }
    }

    #endregion


    public class HelpMenuItem
    {
        public int ID { get; set; }

        public string Name { get; set; }

        public string Url { get; set; }

        public string Description { get; set; }

        public int visibility { get; set; }

        public int order { get; set; }

        public Guid uid { get; set; }

        public bool isEditable { get; set; }

        public bool isSystem { get; set; }
    }

    public class UpdateHelpMenuItem
    {
        public string Name { get; set; }

        public string Url { get; set; }

        public string Description { get; set; }

        public int visibility { get; set; }

        public int order { get; set; }

        public Guid uid { get; set; }
    }

    public class AddHelpMenuItem
    {
        public string Name { get; set; }

        public string Url { get; set; }

        public string Description { get; set; }

        public int visibility { get; set; }

        public int order { get; set; }
    }

    public class DeleteMenuItem
    {
        public Guid uid { get; set; }
    }

    public class HelpMenuItemMessage
    {
        public Guid uid { get; set; }

        public string title { get; set; }

        public string message { get; set; }
    }
}
