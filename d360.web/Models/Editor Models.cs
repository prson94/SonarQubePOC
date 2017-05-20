using d360.core;
using d360.core.entities;
using d360.core.enums;
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
    #region Abstract Classes

    public abstract class BaseEditorModel
    {
        public string FormUri { get; set; }

        public string FormMethod { get; set; }

        public string FormName { get; set; }

        public string FormDescription { get; set; }

        public bool IsUsed { get; set; }
    }

    public class BaseObjectModel
    {
        public string Object { get; set; }
        public int ObjectID { get; set; }
    }

    public abstract class BaseResponsibilityEditorModel : BaseEditorModel
    {
        public BaseResponsibilityEditorModel()
        {
            Contexts = new List<SelectListItem>();
            ResponsibilityTypes = new List<SelectListItem>();
        }

        public Responsibility Responsibility { get; set; }
        public List<SelectListItem> Contexts { get; set; }
        public List<SelectListItem> ResponsibilityTypes { get; set; }
    }

    /// <summary>
    /// Serves as the base editor model for all forms.
    /// </summary>
    //public class EditorModel
    //{
    //    public string Title { get; set; }
    //    public string Description { get; set; }
    //    public string Uri { get; set; }
    //    public string Method { get; set; }
    //    public string Context { get; set; }

    //    public bool HasPermission { get; set; }
    //}

    #endregion

    public class AddItemsToDiagramModel
    {
        public List<AddItemsToDiagramItem> Items { get; set; }
    }

    public class AddItemsToDiagramItem
    {
        public int Position { get; set; }
        public int IntersectTypeID { get; set; }
        public SystemObjects Subject { get; set; }
        public int SubjectID { get; set; }
        public SystemObjects Object { get; set; }
        public int ObjectID { get; set; }
        public int IntersectID { get; set; }
        public string ErrorMessage { get; set; }

        public IntersectDetail Intersect { get; set; }
    }

    public class MapRulesModel
    {
        public List<MapRuleModel> Rules { get; set; }
    }

    public class MapRuleModel
    {
        public int ID { get; set; }
        public int SourceIntersectID { get; set; }
        public string SourceDiagramKey { get; set; }
        public int TargetIntersectID { get; set; }
        public string TargetDiagramKey { get; set; }
        public List<MapRuleItemModel> Sources { get; set; }
        public List<MapRuleItemModel> Targets { get; set; }
        public string Transformation { get; set; }
    }

    public class MapRuleItemModel
    {
        public int ID { get; set; }
        public int IntersectID { get; set; }
        public int FusionAttributeID { get; set; }
        public string FusionAttributeTextPath { get; set; }
    }

    public class ExternalScoreModel
    {
        public decimal Value { get; set; }
    }

    public class SourcePostModel
    {
        public SourcePostModel()
        {
            Adds = new List<SourcePostAddModel>();
            Deletes = new List<SourcePostDeleteModel>();
            Edits = new List<SourcePostEditModel>();
        }

        public List<SourcePostAddModel> Adds { get; set; }
        public List<SourcePostDeleteModel> Deletes { get; set; }
        public List<SourcePostEditModel> Edits { get; set; }
    }

    public class KnockoutDisplayItem
    {
        public string title { get; set; }
        public string value { get; set; }
    }

    public class SourcePostAddModel
    {
        public int SourceIntersectID { get; set; }
        public string SourceKey { get; set; }
        public int TargetIntersectID { get; set; }
        public string TargetKey { get; set; }
        public int IntersectRoleID { get; set; }
        public string Transformation { get; set; }

        /// <summary>
        /// The current object that we are creating sources for.
        /// </summary>
        public string Focal { get; set; }

        /// <summary>
        /// The current object's ID that we are creating sources for.
        /// </summary>
        public int FocalID { get; set; }

        public string Subject { get; set; }
        public int SubjectID { get; set; }
        public string Object { get; set; }
        public int ObjectID { get; set; }
        public int PredicateID { get; set; }
    }

    public class SourcePostDeleteModel
    {
        public int MapID { get; set; }

        /// <summary>
        /// The current object that we are deleting sources for.
        /// </summary>
        public SystemObjects Focal { get; set; }

        /// <summary>
        /// The current object's ID that we are deleting sources for.
        /// </summary>
        public int FocalID { get; set; }

        public int IntersectMapID { get; set; }
    }

    public class SourcePostEditModel
    {
        public int MapID { get; set; }
        public int IntersectRoleID { get; set; }
        public string Transformation { get; set; }


        public int IntersectMapID { get; set; }
        public int PredicateID { get; set; }
    }

    public class SynonymEditModel
    {
        public SystemObjects Type { get; set; }

        public int ID { get; set; }

        public string Synonym { get; set; }

        public bool TypeIsSubject { get; set; }

        public int PredicateID { get; set; }
    }


    public class NymAllocationModel
    {
        public SystemObjects Object { get; set; }

        public int ObjectID { get; set; }

        public int[] PredicateIDs { get; set; }
    }

    public class ArtifactTypeEditorModel : BaseEditorModel
    {
        public ArtifactType ArtifactType { get; set; }

        public string IconBackColor { get; set; }

        public string IconForeColor { get; set; }
    }

    public class IntersectTypePredicateEditorModel : BaseEditorModel
    {
        public int IntersectTypeID { get; set; }

        public List<Predicate> AllocatedPredicates { get; set; }
        public List<Predicate> AvailablePredicates { get; set; }
    }

    public class AttributeTypeEditorModel : BaseEditorModel
    {
        public AttributeTypeEditorModel()
        {
            IsUsed = false;
        }

        public AttributeType AttributeType { get; set; }

        public List<SelectListItem> Tokens { get; set; }

        public List<SelectListItem> AttributeTypeCategories { get; set; }
    }

    public class ClaimsMatrixEditorItemModel
    {
        public Claim Claim { get; set; }
        public ClaimObject ClaimObject { get; set; }
        public int? ID { get; set; }
    }

    public class ClaimsMatrixEditorModel
    {
        public string ObjectType { get; set; }
        public int ObjectID { get; set; }
        public int? ResponsibilityTypeID { get; set; }
        public List<ClaimsMatrixEditorItemModel> Items { get; set; }
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
        public bool DisableIssuePosting { get; set; }        
        public bool DisableIssueManagement { get; set; }
        public string CompanyLogo { get; set; }
        public bool SetLogoToDefault { get; set; }
        public string CurrentCompanyLogoPath { get; set; }
        public string CompanyIcon { get; set; }
        public bool SetIconToDefault { get; set; }
        public string CurrentCompanyIconPath { get; set; }

        public bool UseNewWorkflow { get; set; }

        public string ArtifactType_TaxonomyTypeID { get; set; }
        public string ArtifactType_TaxonomyTypeIDNodes { get; set; }

        public string HeaderBackgroundColor { get; set; }

        public List<CompanySettingsIpRestrictionEditorModel> IpRestrictions { get; set; }
        public List<SiteNav> SiteNav { get; set; } = new List<SiteNav>();
        public string DefaultSearchTypes { get; set; }
    }

    [DataContract(Namespace = constants.NAMESPACE)]
    public class CreateResponse
    {
        [DataMember]
        public string Message { get; set; }
    }

    [DataContract(Namespace = constants.NAMESPACE)]
    public class EditableField : ReadOnlyField
    {
        public EditableField()
        {
            Items = new List<SelectListItem>();
            ReadOnly = false;
            MultiSelect = false;
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

    [DataContract(Name = "item", Namespace = constants.NAMESPACE)]
    public class EditableFieldLookupItem : Dictionary<string, object> { }

    [DataContract(Name = "list", Namespace = constants.NAMESPACE)]
    public class EditableFieldLookupList : List<EditableFieldLookupItem> { }

    [DataContract(Namespace = constants.NAMESPACE)]
    public class EditableForm
    {
        public EditableForm()
        {
            FormSize = EditableForm.FormSize_Medium;
        }
        internal static string FormSize_Small = "small";
        internal static string FormSize_Medium = "medium";
        internal static string FormSize_Large = "large";

        public string Context { get; set; }
        public string FormTitle { get; set; }
        public string FormDescription { get; set; }
        public string FieldUri { get; set; }
        public string FormUri { get; set; }
        public string FormMethod { get; set; }
        public string FormSize { get; set; }
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
    }

    public class FieldLookupRelationItem
    {
        public int IntersectTypeID { get; set; }
        public string Object { get; set; }
        public int ObjectID { get; set; }
        public int RelationType { get; set; }
    }

    public class FieldLookupFieldItem
    {
        public string Object { get; set; }
        public int ObjectID { get; set; }
        public int FieldTypeID { get; set; }
        public string FieldTypeName { get; set; }
        public string Filter { get; set; }
        public string OverrideDisplayName { get; set; }
        public int DisplayOrder { get; set; }
        public int SortOrder { get; set; }
        public bool Show { get; set; } = true;
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

    public class FieldTypeFilteredLookupItemEditorModel
    {
        public int ID { get; set; }
        public string Object { get; set; }
        public int ObjectID { get; set; }
        public ICollection<FieldTypeItemDisplayFieldEditorModel> DisplayFields { get; set; }
        public bool HideHeader { get; set; }
        public bool HideFooter { get; set; }


        public FieldValidity Validation()
        {
            var prefix = "You are missing a";
            var valid = new FieldValidity();
            if (string.IsNullOrEmpty(Object) || ObjectID <= 0)
            {
                valid.Valid = false;
                valid.Message = $"{prefix} field.";
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

    public class FieldTypeEditorModel
    {

        public bool FieldIsUsed { get; set; }

        public FieldType FieldType { get; set; }

        public FieldTypeFilteredLookupItemEditorModel FilteredLookupItem { get; set; }

        public ICollection<FieldTypeFusionItemEditorModel> FusionItems { get; set; }

        public FieldTypeRelationItemEditorModel RelationItem { get; set; }

        public List<FieldTypeRelationItemEditorModel> RelationItems { get; set; }

        public FieldTypeOwnershipLookupEditorModel OwnershipLookupSettings { get; set; }

        public FieldValidity Validation()
        {
            var valid = new FieldValidity();

            if (FieldType.Type == core.DataType.FusionLookup.ToString())
            {
                if (FusionItems == null)
                {
                    valid.Valid = false;
                    valid.Message = "You are missing one or more fusion items.";
                }
                else
                {
                    if (FusionItems.Count == 0)
                    {
                        valid.Valid = false;
                        valid.Message = "You are missing one or more fusion items.";
                    }
                }
            }

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
        /// keyup, blur, focus, change
        /// </summary>
        public string action { get; set; }
        /// <summary>
        /// required; length=3,12; right:0,0; phone; ssn; zipCode; email; inline javascript function
        /// </summary>
        public string rule { get; set; }

        public string regex { get; set; }
    }

    public class FormHeaderEditorModel 
    {
        public FormHeaderEditorModel()
        {
            SaveActionName = "Save";
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public string Context { get; set; }
        public string SaveActionName { get; set; }
    }
       

    public class FusionRuleEditorModel
    {
        public int FusionTypeID { get; set; }

        public int FusionID { get; set; }

        public string FormUri { get; set; }

        public string FormMethod { get; set; }

        public string FormName { get; set; }

        public FusionRule Rule { get; set; }

        public List<FusionAttributeType> AttributeTypes { get; set; }        
    }

    public class FusionRuleFilterFieldEditorModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public class FusionRuleFilterEditorModel
    {
        public FusionRuleFilterEditorModel()
        {
            Items = new List<FusionRuleFilterItem>();
            TextOperators = new List<string>() { "Contains", "Ends With", "Equals", "Starts With", "Does Not Contain", "Does Not End With", "Does Not Equal", "Does Not Start With" };
            BoolOperators = new List<string>() { "Equals" };
            FieldTypes = new List<FusionRuleFilterFieldEditorModel>();
        }

        public int FusionRuleID { get; set; }

        #region These fields will be populated on edit

        public int? ID { get; set; }

        public string Name { get; set; }

        public bool All { get; set; }

        public List<FusionRuleFilterItem> Items { get; set; }

        #endregion

        #region These lists will always be populated

        public List<FusionRuleFilterFieldEditorModel> FieldTypes { get; set; }

        public List<string> TextOperators { get; set; }

        public List<string> BoolOperators { get; set; }

        #endregion
    }

    public class FusionRuleItemEditorModel
    {
        public string FormUri { get; set; }

        public string FormMethod { get; set; }

        public string FormName { get; set; }

        public int FusionID { get; set; }

        public int TargetFusionAttributeTypeID { get; set; }

        public List<FusionRuleItem> Items { get; set; }
    }

    public class FusionRuleStepEditorModel
    {
        public string FormUri { get; set; }

        public string FormMethod { get; set; }

        public string FormName { get; set; }
        
        public FusionRuleStep RuleStep { get; set; }

        public int FusionID { get; set; }

        public int FusionTypeID { get; set; }        
    }

    public class FusionRuleStepMappingEditorModel
    {
        public string FormUri { get; set; }

        public string FormMethod { get; set; }

        public string FormName { get; set; }

        public List<SelectListItem> SourceFields { get; set; }

        public List<SelectListItem> TargetFields { get; set; }

        public FusionRuleStepMapping Item { get; set; }
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

        [Required, RegularExpression(@"^$|\b([A-Za-z0-9_\.-]+)@([\dA-Za-z\.-]+)\.([A-Za-z\.]{2,6})\b"), Display(Name = "Email address")]
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
    }

    public class PeopleResponsibilityEditorModel : BaseResponsibilityEditorModel
    {
        public PeopleResponsibilityEditorModel()
        {
            Resources = new List<SelectListItem>();
        }

        public List<SelectListItem> Resources { get; set; }
    }

    public class HierarchyPostModel : BaseEditorModel
    {
        public HierarchyPostModel()
        {
            IsAddingParent = false;
            GroupNumber = -1;
        }

        public int IntersectID { get; set; }
        public PredicateType HierarchyType { get; set; }
        public int PredicateID { get; set; }
        public bool IsAddingParent { get; set; }

        public int ObjectID { get; set; }
        public string Object { get; set; }
        public string ObjectType { get; set; }
        public int ObjectTypeID { get; set; }
        public int SubjectID { get; set; }
        public string Subject { get; set; }
        public string SubjectType { get; set; }
        public int SubjectTypeID { get; set; }
        public int GroupNumber { get; set; }
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
            //if (Value <= 0)
            //{
            //    valid.Valid = false;
            //    valid.Message += $"{prefix} Value.";
            //}

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

    public class StatisticTypeEditorModel : BaseEditorModel
    {
        public StatisticTypeEditorModel()
        {
            ExistenceCheckItems = new List<SelectListItem>();
            CountCheckItems = new List<SelectListItem>();
            PropertyExistenceCheckItems = new List<SelectListItem>();
            PropertyValueCheckItems = new List<SelectListItem>();
            RelationshipCheckItems = new List<SelectListItem>();
            RollupCheckItems = new List<SelectListItem>();
        }

        public StatisticType StatisticType { get; set; }

        public string ObjectTypeInfo { get; set; }

        public string PropertyName { get; set; }

        public string Value { get; set; }

        //Event metric fields
        public string ValidFieldName { get; set; }
        public string InvalidFieldName { get; set; }
        public string Threshold { get; set; }

        public List<SelectListItem> ExistenceCheckItems { get; set; }

        public List<SelectListItem> CountCheckItems { get; set; }

        public List<SelectListItem> PropertyValueCheckItems { get; set; }

        public List<SelectListItem> PropertyExistenceCheckItems { get; set; }

        public List<SelectListItem> RelationshipCheckItems { get; set; }

        public List<SelectListItem> RollupCheckItems { get; set; }
    }
            
    public class LineageEditorModel
    {
        public SystemObjects Focal { get; set; }
        public int FocalID { get; set; }
        public List<LineageEditorRow> Existing { get; set; }
        public List<LineageEditorRow> Adds { get; set; }
        public List<LineageEditorRow> Deletes { get; set; }
    }

    public class LineageEditorTechnicalModel
    {
        public SystemObjects Focal { get; set; }
        public int FocalID { get; set; }
        public List<LineageEditorTechnicalRow> Existing { get; set; }
        public List<LineageEditorTechnicalRow> Adds { get; set; }
        public List<LineageEditorTechnicalRow> Deletes { get; set; }
    }

    public class LineagePreviewModel
    {
        public LineageEditorModel BusinessModel { get; set; }
        public LineageEditorTechnicalModel TechnicalModel { get; set; }
    }


    public class LineageEditorRow
    {

        public int ID { get; set; }

        public string sourcekey { get; set; }
        public string targetkey { get; set; }

        public SystemObjects FocalObject { get; set; }
        public int FocalID { get; set; }

        public int SourceIntersectID { get; set; }
        public string SourceIntersectTypeName { get; set; }
        public int SourceIntersectTypeID { get; set; }
        public string SourceSubjectName { get; set; }
        public int SourceSubjectID { get; set; }
        public SystemObjects SourceSubject { get; set; }
        public string SourceObjectName { get; set; }
        public int SourceObjectID { get; set; }
        public SystemObjects SourceObject { get; set; }

        public int TargetIntersectID { get; set; }
        public string TargetIntersectTypeName { get; set; }
        public int TargetIntersectTypeID { get; set; }
        public string TargetSubjectName { get; set; }
        public int TargetSubjectID { get; set; }
        public SystemObjects TargetSubject { get; set; }
        public string TargetObjectName { get; set; }
        public int TargetObjectID { get; set; }
        public SystemObjects TargetObject { get; set; }

        public bool HasError { get; set; }
        public string ErrorMessage { get; set; }

        public List<LineageEditorTechnicalRow> TechnicalAdds { get; set; }

        public List<LineageEditorTechnicalRow> TechnicalDeletes { get; set; }

    }

    public class LineageEditorTechnicalRow
    {
        public int ID { get; set; }
        public int MapItemID { get; set; }

        public int SourceFusionAttributeID { get; set; }
        public int TargetFusionAttributeID { get; set; }
        public string SourceFusionAttributeName { get; set; }
        public string TargetFusionAttributeName { get; set; }

        public bool HasError { get; set; } = false;
        public string ErrorMessage { get; set; }
    }
}
