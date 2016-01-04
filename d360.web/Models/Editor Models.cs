using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.workflow;
using d360.workflow.entities;
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

    [DataContract(Namespace = constants.NAMESPACE)]
    public class AddRelationshipsModel
    {
        [DataMember]
        public IntersectClassification Classification { get; set; }

        [DataMember]
        public int? Role { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public List<ObjectModel> Targets { get; set; }
    }

    public class ArtifactTypeEditorModel : BaseEditorModel
    {
        public ArtifactType ArtifactType { get; set; }

        public string IconBackColor { get; set; }

        public string IconForeColor { get; set; }
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
        public bool DisableQuestionPosting { get; set; }
        public string CompanyLogo { get; set; }
        public bool SetLogoToDefault { get; set; }
        public string CurrentCompanyLogoPath { get; set; }
        public string CompanyIcon { get; set; }
        public bool SetIconToDefault { get; set; }
        public string CurrentCompanyIconPath { get; set; }

        public List<CompanySettingsIpRestrictionEditorModel> IpRestrictions { get; set; }
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

    public class EditRelationshipModel
    {
        [DataMember]
        public IntersectClassification Classification { get; set; }

        [DataMember]
        public int IntersectTypeID { get; set; }

        [DataMember]
        public int? Role { get; set; }

        [DataMember]
        public string Description { get; set; }
    }

    public class FieldTypeEditorModel
    {
        public FieldTypeEditorModel()
        {
            FieldIsUsed = false;
        }

        public bool FieldIsUsed { get; set; }

        public string FormUri { get; set; }

        public string FormMethod { get; set; }

        public string FormName { get; set; }

        public FieldType FieldType { get; set; }

        public List<SelectListItem> DataTypes
        {
            get
            {
                var t = d360.core.DataType.Boolean;
                return t.GetDataTypeInfoList()
                    .Where(i => !i.ReadOnly)
                    .Select(i => new SelectListItem
                    {
                        Text = i.Description,
                        Value = i.Name,
                        Selected = false
                    })
                    .OrderBy(i => i.Text)
                    .ToList();
            }
        }

        public List<SelectListItem> LookupLists { get; set; }
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

    public class FusionAttributeTypeEditorControl
    {
        public string Title { get; set; }
        public List<FusionAttributeType> Types { get; set; }
        public int? SelectedID { get; set; }
    }

    public class FusionOwnerEditModel
    {
        public string ObjectType { get; set; }

        public int? ObjectID { get; set; }

        public string ParentObjectType { get; set; }

        public int? ParentObjectID { get; set; }
    }

    public class FusionOwnerEditListModel
    {
        public string RelationshipOwnerObjectType { get; set; }

        public int RelationshipOwnerObjectID { get; set; }

        public int FusionID { get; set; }

        public List<FusionOwnerEditModel> Items { get; set; }
    }

    public class FusionOwnerRuleEditorModel
    {
        public bool IsUsed { get; set; }

        public int FusionTypeID { get; set; }

        public int FusionID { get; set; }

        public string FormUri { get; set; }

        public string FormMethod { get; set; }

        public string FormName { get; set; }

        public FusionAttributeOwnerRule Rule { get; set; }

        public List<FusionAttributeType> AttributeTypes { get; set; }
    }

    public class FusionOwnerRuleItemEditorModel
    {
        public string FormUri { get; set; }

        public string FormMethod { get; set; }

        public string FormName { get; set; }

        public int FusionID { get; set; }

        public int TargetFusionAttributeTypeID { get; set; }

        public FusionAttributeOwnerRuleItem Item { get; set; }
    }

    public class FusionPromotionEditModel
    {
        public string ObjectType { get; set; }

        public int? ObjectID { get; set; }

        public string ParentObjectType { get; set; }

        public int? ParentObjectID { get; set; }
    }

    //public class FusionPromotionEditListModel
    //{
    //    public int FusionID { get; set; }

    //    public string PromotionObjectType { get; set; }

    //    public int PromotionObjectID { get; set; }

    //    public string PromotionParentObjectType { get; set; }

    //    public int PromotionParentObjectID { get; set; }

    //    public bool Enabled { get; set; }

    //    public List<FusionPromotionEditModel> Items { get; set; }
    //}

    public class FusionPromotionRuleEditorModel
    {
        public int FusionTypeID { get; set; }

        public int FusionID { get; set; }

        public string FormUri { get; set; }

        public string FormMethod { get; set; }

        public string FormName { get; set; }

        public FusionAttributePromotionRule Rule { get; set; }

        public List<FusionAttributeType> AttributeTypes { get; set; }

        public int ParentTypeID { get; set; }
    }

    public class FusionPromotionRuleItemEditorModel
    {
        public string FormUri { get; set; }

        public string FormMethod { get; set; }

        public string FormName { get; set; }

        public int FusionID { get; set; }

        public int TargetFusionAttributeTypeID { get; set; }

        public FusionAttributePromotionRuleItem Item { get; set; }
    }

    public class FusionPromotionRuleMappingEditorModel
    {
        public string FormUri { get; set; }

        public string FormMethod { get; set; }

        public string FormName { get; set; }

        public List<SelectListItem> SourceFields { get; set; }

        public List<SelectListItem> TargetFields { get; set; }

        public FusionAttributePromotionRuleMapping Item { get; set; }
    }

    public class IntersectTypeRoleEditorModel
    {
        public int? RoleID { get; set; }
        public string NewRoleName { get; set; }
        public string Side1Label { get; set; }
        public string Side2Label { get; set; }
    }

    public class IntersectTypeEditorModel
    {
        public IntersectTypeEditorModel()
        {
            Roles = new List<IntersectTypeRoleEditorModel>();
        }

        public int ID { get; set; }

        public string Side1 { get; set; }
        public string Side1DisplayText { get; set; }

        public string Side2 { get; set; }
        public string Side2DisplayText { get; set; }

        public List<IntersectTypeRoleEditorModel> Roles { get; set; }

        /// <summary>
        /// Should certain fields be made read-only based on whether any 
        /// relationships exist for this type.
        /// </summary>
        public bool LimitedChangesOnly { get; set; }
    }
    
    //public class LoadTypeRuleEditorModel
    //{
    //    public int? ID { get; set; }
    //    public int LoadTypeID { get; set; }

    //    public bool LookupTypeRuleGroupsEnabled { get; set; }
    //    public List<SelectListItem> LookupTypeRuleGroups { get; set; }

    //    public List<SelectListItem> Objects { get; set; }

    //    public List<SelectListItem> Fields { get; set; }
    //}

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

    public class PeopleResponsibilityEditorModel : BaseResponsibilityEditorModel
    {
        public PeopleResponsibilityEditorModel()
        {
            Resources = new List<SelectListItem>();
        }

        public List<SelectListItem> Resources { get; set; }
    }

    [DataContract(Name = "QuestionResponse", Namespace = constants.NAMESPACE)]
    public class QuestionResponseModel
    {
        [DataMember]
        public int QuestionTypeID { get; set; }
        [DataMember]
        public int SurveyTypeID { get; set; }
        [DataMember]
        public SystemObjects ObjectType { get; set; }
        [DataMember]
        public int ObjectID { get; set; }
        [DataMember]
        public int Value { get; set; }
        [DataMember]
        public string Comment { get; set; }
    }

    public class ReportEditorModel : BaseEditorModel
    {
        public string FormDirections { get; set; }

        public Report Report { get; set; }

        public List<SelectListItem> ReportLayouts { get; set; }

        public List<SelectListItem> ObjectTypes { get; set; }
    }

    public class ReportTileEditorModel : BaseEditorModel
    {
        public string FormDirections { get; set; }

        public string ReportBaseUri { get; set; }

        public ReportTile ReportTile { get; set; }

        public List<SelectListItem> ReportTileTypes { get; set; }

        public List<SelectListItem> ContentAreaNumbers { get; set; }

        public List<ReportSchemaModel> SchemaItems { get; set; }

        public List<SelectListItem> ObjectTypes { get; set; }
    }

    public class SourcingResponsibilityEditorModel : BaseResponsibilityEditorModel
    {
        public SourcingResponsibilityEditorModel()
        {
            Artifacts = new List<SelectListItem>();
            SourceResponsibilities = new List<SelectListItem>();
        }

        public List<SelectListItem> Artifacts { get; set; }

        public List<SelectListItem> SourceResponsibilities { get; set; }

        public int BusinessTransformationID { get; set; }
        public string BusinessTransformation { get; set; }

        public int TechnicalTransformationID { get; set; }
        public string TechnicalTransformation { get; set; }
    }

    //public class SourcingResponsibilityTypeEditorModel
    //{
    //    public int ID { get; set; }

    //    public string Name { get; set; }

    //    public string Description { get; set; }

    //    public ResponsibilityTypeGroup ResponsibilityTypeGroup { get; set; }

    //    public List<EditableFieldItem> ArtifactTypes { get; set; }
    //}

    public class SourceToTargetEditForm : EditableForm
    {
        public string Object { get; set; }
        public int ObjectID { get; set; }
        public string ObjectName { get; set; }
    }

    public class SourceToTargetEnvironmentEditModel
    {
        public string Object { get; set; }
        public int ObjectID { get; set; }
    }
    public class SourceToTargetGroupEditModel
    {
        public string Definition { get; set; }
        public string Formula { get; set; }
        public List<SourceToTargetGroupItemEditModel> Items { get; set; }
    }
    public class SourceToTargetGroupItemEditModel
    {
        public string SourceSystem { get; set; }
        public string SourceObject { get; set; }
        public int SourceFusionAttribute { get; set; }

        public string TargetSystem { get; set; }
        public string TargetObject { get; set; }
        public int TargetFusionAttribute { get; set; }
    }
    public class SourceToTargetRelationshipEditModel
    {
        public string Object { get; set; }
        public int ObjectID { get; set; }
    }

    public class SourceToTargetEditModel
    {
        public string Object { get; set; }
        public int ObjectID { get; set; }
        public List<SourceToTargetEnvironmentEditModel> Environments { get; set; }
        public List<SourceToTargetGroupEditModel> Groups { get; set; }
        public List<SourceToTargetRelationshipEditModel> Relationships { get; set; }
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

    public class WorkflowAllocationEditorModel
    {
        public WorkflowAllocationEditorModel()
        {
            Properties = new Dictionary<string, string>();
            Responsibilities = new List<SelectListItem>();
        }

        public WorkflowType WorkflowType { get; set; }

        public string ObjectType { get; set; }

        public int ObjectID { get; set; }

        public bool Enabled { get; set; }

        public bool Required { get; set; }

        public List<SelectListItem> Responsibilities { get; set; }

        public Dictionary<string, string> Properties { get; set; }
    }

    public class WorkflowTypeRelationEditorModel : BaseEditorModel
    {
        public WorkflowTypeRelationEditorModel()
        {
            Enabled = true;
            ObjectTypes = new List<SelectListItem>();
            ParentTypes = new List<SelectListItem>();
            ResponsibilityTypes = new List<SelectListItem>();
        }

        public bool Enabled { get; set; }

        public WorkflowType WorkflowType { get; set; }

        public WorkflowTypeRelation WorkflowTypeRelation { get; set; }

        public List<SelectListItem> ObjectTypes { get; set; }

        public List<SelectListItem> ParentTypes { get; set; }

        public List<SelectListItem> ResponsibilityTypes { get; set; }
    }
}
