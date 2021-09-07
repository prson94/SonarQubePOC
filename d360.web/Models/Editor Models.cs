using d360.core;
using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

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
        public bool EnableSearchExactMatch { get; set; }

        public string HeaderBackgroundColor { get; set; }

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
        public bool UseNativeLookupControl
        {
            get
            {
                return !(VirtualScroll || UseTypeahead);
            }
        }
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

    public class FieldTypeFusionItemEditorModel
    {
        public int ID { get; set; }
        public int SourceFusionAttributeType { get; set; }
        public int ReferenceType { get; set; }
        public int? TargetFusionAttributeType { get; set; }
        public ICollection<FieldTypeItemDisplayFieldEditorModel> DisplayFields { get; set; }
        public bool HideHeader { get; set; }
        public bool HideFooter { get; set; }

        public FieldValidity Validation()
        {
            var prefix = "You are missing a";
            var valid = new FieldValidity();
            if (SourceFusionAttributeType <= 0)
            {
                valid.Valid = false;
                valid.Message = $"{prefix} target item.";
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
                    if (ReferenceType > 1 && !TargetFusionAttributeType.HasValue)
                    {
                        valid.Valid = false;
                        valid.Message = $"{prefix} reference item.";
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

        public ICollection<FieldTypeFusionItemEditorModel> FusionItems { get; set; }

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
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public class RegisterModel
    {
        public RegisterStep Step { get; set; }

        [Required, RegularExpression(@"^$|\b([A-Za-z0-9'_\.-]+)@([\dA-Za-z\.-]+)\.([A-Za-z\.]{2,6})\b"), Display(Name = "Email address")]
        public string Email { get; set; }

        [Display(Name = "First name")]
        public string FirstName { get; set; }

        [Display(Name = "Last name")]
        public string LastName { get; set; }

        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; }

        public bool? Accept { get; set; }

        public Guid? RegistrationID { get; set; }

        public string Message { get; set; }

        public List<ContractRegisterModel> Contracts { get; set; }

        public bool IsUsingActiveDirectory { get; set; }

        [Display(Name = "Job Title")]
        public string Title { get; set; }
    }

    public class TermsModel
    {
        public TermsModel() { }

        public TermsModel(Contract contract)
        {
            this.Contract = contract;
            this.Acceptance = new ContractAcceptance();
            this.Acceptance.ContractID = contract.ID;
        }

        public TermsModel(Contract contract, string redirectUri, bool isLastContract) : this(contract)
        {
            this.RedirectUri = redirectUri;
            this.IsLastContract = isLastContract;
        }
        public Contract Contract { get; set; }
        public ContractAcceptance Acceptance { get; set; }
        public string RedirectUri { get; set; } = null;

        public bool IsLastContract { get; set; } = false;

        public bool IsLastOrgContract { get; set; } = false;

        public List<int> OrgsWithContracts { get; set; } = new List<int>();
    }

    public class ContractRegisterModel
    {
        public ContractRegisterModel() { }

        public ContractRegisterModel(Contract contract)
        {
            Contract = contract;
            ContractAcceptance = new ContractAcceptance
            {
                ContractID = contract.ID,
                Accepted = false
            };
            Accept = false;
        }

        public Contract Contract { get; set; }
        public ContractAcceptance ContractAcceptance { get; set; }

        public bool Accept { get; set; } = false;

        public bool IsAccepted
        {
            get
            {
                return this.ContractAcceptance?.Accepted ?? false;
            }
        }

    }

    public class QuestionTypeItemEditorModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int Value { get; set; }

        public FieldValidity Validation()
        {
            var prefix = "You are missing a";
            var valid = new FieldValidity();
            if (string.IsNullOrEmpty(Name))
            {
                valid.Valid = false;
                valid.Message += $"{prefix} Name.";
            }


            return valid;
        }
    }

    public class QuestionTypeEditorModel
    {
        public bool LimitedChangesOnly { get; set; }

        public int ID { get; set; }
        public int SurveyTypeID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public QuestionDisplayStyle DisplayStyle { get; set; }

        public List<KnockoutDisplayItem> DisplayStyleOptions { get; set; }

        public List<QuestionTypeItemEditorModel> Items { get; set; }

        public FieldValidity Validation()
        {
            var valid = new FieldValidity();

            if (Items == null)
            {
                valid.Valid = false;
                valid.Message = "You are missing one or more items.";
            }
            else
            {
                if (Items.Count == 0)
                {
                    valid.Valid = false;
                    valid.Message = "You are missing one or more items.";
                }
            }

            return valid;
        }
    }
    public class AddGroup
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid PrimaryOwnerUid { get; set; }
        public Guid SecondaryOwnerUid { get; set; }
    }

    public class UpdateGroup
    {
        public Guid Uid { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid PrimaryOwnerUid { get; set; }
        public Guid SecondaryOwnerUid { get; set; }
    }

    #region Asset Browser

    public enum AssetBrowserApiHopDirection
    {
        None = 0,
        Forward = 1,
        Backward = 2,
        Both = 3
    }

    public enum AssetBrowserApiHopType
    {
        Lineage = 1,
        Impact = 2
    }

    public enum AssetBrowserDiagramType
    {
        Lineage = 1,
        Impact = 2,
        Process = 3
    }

    [DataContract]
    public class AssetBrowserApiHopRequestModel
    {
        [DataMember]
        public bool Initial { get; set; }

        [DataMember]
        public List<AssetBrowserApiHopAssetRequestModel> Assets { get; set; }

        [DataMember]
        public List<AssetBrowserApiHopIgnoreRequestModel> RelationsToIgnore { get; set; }

        [DataMember]
        public AssetBrowserApiHopDirection Direction { get; set; } = AssetBrowserApiHopDirection.Both;

        [DataMember]
        public AssetBrowserDiagramType DiagramType { get; set; } = AssetBrowserDiagramType.Impact;

        [DataMember]
        public AssetBrowserApiHopType HopType { get; set; } = AssetBrowserApiHopType.Impact;

        [DataMember]
        public Guid? PredicateUid { get; set; }

        [DataMember]
        public int Hops { get; set; } = 3;

        [DataMember]
        public bool LeafOnly { get; set; } = true;
    }

    [DataContract]
    public class AssetBrowserApiOwnerHopRequestModel
    {
        [DataMember]
        public List<AssetBrowserApiHopAssetRequestModel> Assets { get; set; }

        [DataMember]
        public int ResponsibilityTypeId { get; set; }
    }

    [DataContract]
    public class AssetBrowserApiHopAssetRequestModel
    {
        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public string Key { get; set; }
    }

    [DataContract]
    public class AssetBrowserApiHopIgnoreRequestModel
    {
        [DataMember]
        public Guid Uid { get; set; }
    }

    #endregion
}
