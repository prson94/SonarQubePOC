using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FieldType : BaseIntObject, IIntObject
    {
        [DataMember]
        public int? AssetTypeID { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description"), StringLength(250)]
        public string Name { get; set; }

        [DataMember]
        public string Category { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "DisplayDescription_Name", Description = "DisplayDescription_Description")]
        public string DisplayDescription { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "FormDescription_Name", Description = "FormDescription_Description")]
        public string FormDescription { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "ValidationDescription_Name", Description = "ValidationDescription_Description")]
        public string ValidationDescription { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "FriendlyName_Name", Description = "FriendlyName_Description"), StringLength(250)]
        public string FriendlyName { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Type_Name", Description = "Type_Description"), Column(TypeName = "varchar"), StringLength(25)]
        public string Type { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "LookupObjectType_Name", Description = "LookupObjectType_Description"), Column(TypeName = "varchar"), StringLength(25)]
        public string LookupObjectType { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "LookupObjectID_Name", Description = "LookupObjectID_Description")]
        public int? LookupObjectID { get; set; }

        [DataMember]
        public int? LookupObjectFieldTypeID { get; set; }

        [DataMember, StringLength(250)]
        public string LookupDisplayFormat { get; set; }

        [DataMember, StringLength(250)]
        public string LookupEditFormat { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Length_Name", Description = "Length_Description")]
        public int? Length { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "MinimumLength_Name", Description = "MinimumLength_Description")]
        public decimal? MinimumLength { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "MaximumLength_Name", Description = "MaximumLength_Description")]
        public decimal? MaximumLength { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Pattern_Name", Description = "Pattern_Description")]
        [Column(TypeName = "varchar"), StringLength(1000)]
        public string Pattern { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public bool IsListable { get; set; }

        [DataMember]
        public bool IsRequired { get; set; }

        [DataMember]
        public bool IsDisplayable { get; set; }

        [DataMember]
        public bool IsEditable { get; set; }

        [DataMember]
        public bool IsPartOfKey { get; set; }

        [DataMember]
        public int ColumnOrder { get; set; }

        [DataMember]
        public int? ColumnWidth { get; set; }

        [DataMember]
        public int SortOrder { get; set; }

        [DataMember]
        public string DefaultValue { get; set; }
                
        public string DefaultFormattedValue { get; set; }

        [DataMember]
        public bool AllowAllValue { get; set; }

        [DataMember]
        public string AllowAllLabel { get; set; }

        [DataMember]
        public bool IsPrimaryFilter { get; set; }

        [DataMember]
        public bool AllowMultipleValues { get; set; }


        [DataMember]
        public int ParentFieldTypeID { get; set; }

        [DataMember]
        public int UpdatedBy { get; set; }

        [DataMember]
        public decimal? Increment { get; set; }

        [DataMember]
        public int? Precision { get; set; }

        [DataMember]
        public int? FilterPredicateID { get; set; }

        [DataMember]
        public bool? FilterPredicateDirection { get; set; }

        [DataMember]
        public int? FilterFieldTypeID { get; set; }

        [DataMember]
        public bool ShowIfEmpty { get; set; }

        [IgnoreDataMember, ForeignKey("FieldTypeID")]
        public virtual ICollection<Field> Fields { get; set; }

        [IgnoreDataMember]
        public virtual FieldTypeLookup FieldTypeLookup { get; set; }

        [IgnoreDataMember, ForeignKey("FieldTypeID")]
        public virtual ICollection<FieldTypeFilteredLookupDefinition> FieldTypeFilteredLookupDefinitions { get; set; }


        [IgnoreDataMember, ForeignKey("FieldTypeID")]
        public virtual ICollection<FieldTypeFusionLookupDefinition> FieldTypeFusionLookupDefinitions { get; set; }
    }

    #region Data Type models for API

    public class FieldTypeDescriptionApiViewModel_Display
    {
        [DataMember]
        public string Display { get; set; }
    }

    public class FieldTypeDescriptionApiViewModel_DisplayForm: FieldTypeDescriptionApiViewModel_Display
    {
        [DataMember]
        public string Form { get; set; }
    }

    public class FieldTypeDescriptionApiViewModel_Validation
    {
        [DataMember]
        public bool IsRequired { get; set; }
        [DataMember]
        public string Message { get; set; }
    }
    public class FieldTypeDescriptionApiViewModel_ValidationLength : FieldTypeDescriptionApiViewModel_Validation
    {
        [DataMember]
        public decimal? MinimumLength { get; set; }
        [DataMember]
        public decimal? MaximumLength { get; set; }
        [DataMember]
        public int? Length { get; set; }
    }
    public class FieldTypeDescriptionApiViewModel_ValidationDecimal: FieldTypeDescriptionApiViewModel_ValidationLength
    {
        [DataMember]
        public short? Precision { get; set; }
    }
    public class FieldTypeDescriptionApiViewModel_ValidationText : FieldTypeDescriptionApiViewModel_ValidationLength
    {
        [DataMember]
        public string Pattern { get; set; }
    }


    public class FieldTypeEditableApiViewModel
    {
        [DataMember]
        public int ColumnOrder { get; set; }
        [DataMember]
        public int? ColumnWidth { get; set; }
        [DataMember]
        public int SortOrder { get; set; }
        [DataMember]
        public bool IsDisplayable { get; set; }
        [DataMember]
        public bool IsEditable { get; set; }
        [DataMember]
        public bool IsListable { get; set; }
        [DataMember]
        public bool IsPartOfKey { get; set; }
        [DataMember]
        public bool IsPrimaryFilter { get; set; }
        [DataMember]
        public bool ShowIfEmpty { get; set; }
    }

    public class FieldTypeDataTypeBooleanApiViewModel: FieldTypeEditableApiViewModel
    {
        [DataMember]
        public bool? DefaultValue { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_DisplayForm Description { get; set; }
    }

    public class FieldTypeDataTypeComputedFusionLookupApiViewModel
    {
        [DataMember]
        public int ColumnOrder { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Display Description { get; set; }
        [DataMember]
        public bool IsDisplayable { get; set; }
    }

    public class FieldTypeDataTypeComputedOwnershipLookupApiViewModel
    {
        [DataMember]
        public int ColumnOrder { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Display Description { get; set; }
        [DataMember]
        public FieldTypeOwnershipLookupDefinition Definition { get; set; }
        [DataMember]
        public bool IsDisplayable { get; set; }
        [DataMember]
        public bool ShowIfEmpty { get; set; }
    }

    public class FieldTypeDataTypeComputedRelationshipFieldApiViewModel
    {
        [DataMember]
        public int ColumnOrder { get; set; }
        [DataMember]
        public int? ColumnWidth { get; set; }
        [DataMember]
        public int SortOrder { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Display Description { get; set; }
        [DataMember]
        public Guid IntersectTypeUid { get; set; }
        [DataMember]
        public string FieldTypeName { get; set; }
        [DataMember]
        public bool IsDisplayable { get; set; }
        [DataMember]
        public bool IsListable { get; set; }
        [DataMember]
        public bool ShowIfEmpty { get; set; }
    }

    public class FieldTypeDataTypeComputedRelationshipLookupApiViewModel_Field
    {
        [DataMember]
        public Guid AssetTypeUid { get; set; }
        [DataMember]
        public string FieldTypeName { get; set; }
        [DataMember]
        public string Filter { get; set; }
        [DataMember]
        public string OverrideDisplayName { get; set; }
        [DataMember]
        public int DisplayOrder { get; set; }
        [DataMember]
        public int SortOrder { get; set; }
        [DataMember]
        public bool Show { get; set; }
        [DataMember]
        public int Width { get; set; }
    }
    public class FieldTypeDataTypeComputedRelationshipLookupApiViewModel_Relation
    {
        [DataMember]
        public Guid IntersectTypeUid { get; set; }
        [DataMember]
        public Guid AssetTypeUid { get; set; }
        [DataMember]
        public ComplexLookupRelationType RelationType { get; set; }
        [DataMember]
        public short Direction { get; set; }
    }
    public class FieldTypeDataTypeComputedRelationshipLookupApiViewModel
    {
        [DataMember]
        public int ColumnOrder { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Display Description { get; set; }
        [DataMember]
        public FieldTypeComplexLookupDefinition Definition { get; set; }
        [DataMember]
        public bool IsDisplayable { get; set; }
        [DataMember]
        public bool ShowIfEmpty { get; set; }
    }

    public class FieldTypeDataTypeComputedRelationshipReferenceListApiViewModel
    {
        [DataMember]
        public int ColumnOrder { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Display Description { get; set; }
        [DataMember]
        public Guid IntersectTypeUid { get; set; }
        [DataMember]
        public bool IsDisplayable { get; set; }
        [DataMember]
        public bool ShowIfEmpty { get; set; }
    }

    public class FieldTypeDataTypeDateApiViewModel : FieldTypeEditableApiViewModel
    {
        [DataMember]
        public DateTime? DefaultValue { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_DisplayForm Description { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Validation Validation { get; set; }
    }

    public class FieldTypeDataTypeDecimalApiViewModel : FieldTypeEditableApiViewModel
    {
        [DataMember]
        public decimal? DefaultValue { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_DisplayForm Description { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_ValidationDecimal Validation { get; set; }
    }

    public class FieldTypeDataTypeDateTimeApiViewModel : FieldTypeEditableApiViewModel
    {
        [DataMember]
        public DateTime? DefaultValue { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_DisplayForm Description { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Validation Validation { get; set; }
    }
    public class FieldTypeDataTypeHtmlApiViewModel : FieldTypeEditableApiViewModel
    {
        [DataMember]
        public string DefaultValue { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_DisplayForm Description { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_ValidationLength Validation { get; set; }
    }
    public class FieldTypeDataTypeJsonApiViewModel
    {
        [DataMember]
        public int ColumnOrder { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Display Description { get; set; }
        [DataMember]
        public bool IsDisplayable { get; set; }
        [DataMember]
        public bool ShowIfEmpty { get; set; }
    }
    public class FieldTypeDataTypeLinkApiViewModel_DefaultValue
    {
        [DataMember]
        public string Text { get; set; }
        [DataMember]
        public string Url { get; set; }
    }
    public class FieldTypeDataTypeLinkApiViewModel : FieldTypeEditableApiViewModel
    {
        [DataMember]
        public FieldTypeDataTypeLinkApiViewModel_DefaultValue DefaultValue { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_DisplayForm Description { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Validation Validation { get; set; }
    }

    public class FieldTypeDataTypeLookupApiViewModel_Filter
    {
        [DataMember]
        public string FieldTypeName { get; set; }
        [DataMember]
        public Guid? PredicateUid { get; set; }
        [DataMember]
        public bool? UseDirection { get; set; }
    }
    public class FieldTypeDataTypeLookupApiViewModel_Format
    {
        [DataMember]
        public string Display { get; set; }
        [DataMember]
        public string Edit { get; set; }
    }
    public class FieldTypeDataTypeLookupApiViewModel_List
    {
        [DataMember]
        public Guid Uid { get; set; }
        [DataMember]
        public bool AllowMultipleValues { get; set; }
    }
    public class FieldTypeDataTypeLookupApiViewModel : FieldTypeEditableApiViewModel
    {
        [DataMember]
        public string DefaultValue { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_DisplayForm Description { get; set; }
        [DataMember]
        public bool AllowAllValue { get; set; }
        [DataMember]
        public string AllowAllLabel { get; set; }
        [DataMember]
        public FieldTypeDataTypeLookupApiViewModel_Filter Filter { get; set; }
        [DataMember]
        public FieldTypeDataTypeLookupApiViewModel_Format Format { get; set; }
        [DataMember]
        public FieldTypeDataTypeLookupApiViewModel_List List { get; set; }
    }

    public class FieldTypeDataTypeNumberApiViewModel : FieldTypeEditableApiViewModel
    {
        [DataMember]
        public int? DefaultValue { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_DisplayForm Description { get; set; }
        [DataMember]
        public decimal? Increment { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_ValidationLength Validation { get; set; }
    }

    public class FieldTypeDataTypeRelationshipApiViewModel : FieldTypeEditableApiViewModel
    {
        [DataMember]
        public FieldTypeDescriptionApiViewModel_DisplayForm Description { get; set; }
        [DataMember]
        public Guid IntersectTypeUid { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Validation Validation { get; set; }
    }

    public class FieldTypeDataTypeTextApiViewModel : FieldTypeEditableApiViewModel
    {
        [DataMember]
        public string DefaultValue { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_DisplayForm Description { get; set; }
        [DataMember]
        public Guid IntersectTypeUid { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_ValidationText Validation { get; set; }
    }

    public class FieldTypeDataTypeApiViewModel
    {
        [DataMember]
        public FieldTypeDataTypeBooleanApiViewModel Boolean { get; set; }
        [DataMember]
        public FieldTypeDataTypeComputedFusionLookupApiViewModel ComputedFusionLookup { get; set; }
        [DataMember]
        public FieldTypeDataTypeComputedOwnershipLookupApiViewModel ComputedOwnershipLookup { get; set; }
        [DataMember]
        public FieldTypeDataTypeComputedRelationshipFieldApiViewModel ComputedRelationshipField { get; set; }
        [DataMember]
        public FieldTypeDataTypeComputedRelationshipLookupApiViewModel ComputedRelationshipLookup { get; set; }
        [DataMember]
        public FieldTypeDataTypeComputedRelationshipReferenceListApiViewModel ComputedRelationshipReferenceList { get; set; }
        [DataMember]
        public FieldTypeDataTypeDateApiViewModel Date { get; set; }
        [DataMember]
        public FieldTypeDataTypeDateTimeApiViewModel DateTime { get; set; }
        [DataMember]
        public FieldTypeDataTypeDecimalApiViewModel Decimal { get; set; }
        [DataMember]
        public FieldTypeDataTypeHtmlApiViewModel Html { get; set; }
        [DataMember]
        public FieldTypeDataTypeJsonApiViewModel Json { get; set; }
        [DataMember]
        public FieldTypeDataTypeLinkApiViewModel Link { get; set; }
        [DataMember]
        public FieldTypeDataTypeLookupApiViewModel Lookup { get; set; }
        [DataMember]
        public FieldTypeDataTypeNumberApiViewModel Number { get; set; }
        [DataMember]
        public FieldTypeDataTypeRelationshipApiViewModel Relationship { get; set; }
        [DataMember]
        public FieldTypeDataTypeTextApiViewModel Text { get; set; }
    }

    #endregion

    public class FieldTypeApiViewModel
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string FriendlyName { get; set; }
        [DataMember]
        public string Category { get; set; }
        [DataMember]
        public FieldTypeDataTypeApiViewModel Type { get; set; }
    }

    public class FieldTypesApiViewModel
    {
        [DataMember]
        public int pageSize { get; set; } = 250;
        [DataMember]
        public int pageNum { get; set; } = 1;
        [DataMember]
        public int total { get; set; } = 0;
        [DataMember]
        public List<FieldTypeApiViewModel> items { get; set; }
    }
}
