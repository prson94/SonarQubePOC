using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using d360.core.enums;

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

        [DataMember]
        public string Definition { get; set; } = "{}";

        [DataMember]
        public int? ScoreType { get; set; }

        [DataMember]
        public bool SearchAddToResult { get; set; }

        [DataMember]
        public string SearchPrefix { get; set; }

        [DataMember]
        public string SearchSuffix { get; set; }

        [DataMember]
        public int? SearchDisplayOrder { get; set; }

        [IgnoreDataMember, ForeignKey("FieldTypeID")]
        public virtual ICollection<Field> Fields { get; set; }

        [IgnoreDataMember]
        public virtual FieldTypeLookup FieldTypeLookup { get; set; }

        [DataMember]
        public string CounterPrefix { get; set; }

        [DataMember]
        public int? CounterInitialIndex { get; set; }

        [DataMember]
        public bool? DisplayInColumn { get; set; }
    }

    #region Definition property models

    public class FieldTypeDefinition_JsonElement
    {
        public int FieldTypeID { get; set; }

        public string Path { get; set; }

        public string DataType { get; set; }
    }

    #endregion

    #region Data Type models for API

    public class FieldTypeDescriptionApiViewModel_Display
    {
        [DataMember]
        public string Display { get; set; }
    }

    public class FieldTypeDescriptionApiViewModel_DisplayForm : FieldTypeDescriptionApiViewModel_Display
    {
        [DataMember]
        public string Form { get; set; }
    }

    public class FieldTypeDescriptionApiViewModel_Validation
    {
        [DataMember]
        public bool IsRequired { get; set; }
    }
    public class FieldTypeDescriptionApiViewModel_ValidationLength : FieldTypeDescriptionApiViewModel_Validation
    {
        [DataMember]
        public decimal? MinimumLength { get; set; }
        [DataMember]
        public decimal? MaximumLength { get; set; }
    }
    public class FieldTypeDescriptionApiViewModel_ValidationMinMaxValue : FieldTypeDescriptionApiViewModel_Validation
    {
        [DataMember]
        public decimal? MinimumValue { get; set; }
        [DataMember]
        public decimal? MaximumValue { get; set; }
    }
    public class FieldTypeDescriptionApiViewModel_ValidationDecimal : FieldTypeDescriptionApiViewModel_ValidationMinMaxValue
    {
        [DataMember]
        public short? Precision { get; set; }
    }
    public class FieldTypeDescriptionApiViewModel_ValidationText : FieldTypeDescriptionApiViewModel_ValidationLength
    {
        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public string Pattern { get; set; }
    }

    public class FieldTypeDescriptionApiViewModel_Search
    {
        [DataMember]
        public bool AddToResult { get; set; }
        [DataMember]
        public string Prefix { get; set; }
        [DataMember]
        public string Suffix { get; set; }
        [DataMember]
        public int? DisplayOrder { get; set; }
    }

    public class FieldTypeEditableApiViewModel
    {
        [DataMember]
        public int? ColumnOrder { get; set; }
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

    public class FieldTypeDataTypeBooleanApiViewModel : FieldTypeEditableApiViewModel
    {
        [DataMember]
        public bool? DefaultValue { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_DisplayForm Description { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Validation Validation { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Search Search { get; set; }

        [DataMember]
        public bool? DisplayInColumn { get; set; }
    }

    public class FieldTypeDataTypeComputedOwnershipLookupApiViewModel
    {
        [DataMember]
        public int? ColumnOrder { get; set; }
        [DataMember]
        public int? ColumnWidth { get; set; }
        [DataMember]
        public int SortOrder { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Display Description { get; set; }
        [DataMember]
        public FieldTypeOwnershipLookupDefinition Definition { get; set; }
        [DataMember]
        public bool IsDisplayable { get; set; }
        [DataMember]
        public bool IsListable { get; set; }
        [DataMember]
        public bool ShowIfEmpty { get; set; }

        [DataMember]
        public bool HideFilter { get; set; }
        [DataMember]
        public bool HideFooter { get; set; }
        [DataMember]
        public bool HideHeader { get; set; }

        [DataMember]
        public bool? DisplayInColumn { get; set; }
    }

    public class FieldTypeDataTypeComputedRelationshipFieldApiViewModel
    {
        [DataMember]
        public int? ColumnOrder { get; set; }
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
        [DataMember]
        public bool IsPrimaryFilter { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Search Search { get; set; }
        [DataMember]
        public bool? DisplayInColumn { get; set; }
    }

    public class FieldTypeDataTypeComputedRelationshipLookupApiViewModel
    {
        [DataMember]
        public int? ColumnOrder { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Display Description { get; set; }
        [DataMember]
        public FieldTypeComplexLookupDefinitionApiViewModel Definition { get; set; }
        [DataMember]
        public bool IsDisplayable { get; set; }
        [DataMember]
        public bool ShowIfEmpty { get; set; }

        [DataMember]
        public bool HideFilter { get; set; }
        [DataMember]
        public bool HideFooter { get; set; }
        [DataMember]
        public bool HideHeader { get; set; }
    }

    public class FieldTypeDataTypeComputedRelationshipReferenceListApiViewModel
    {
        [DataMember]
        public int? ColumnOrder { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Display Description { get; set; }
        [DataMember]
        public Guid IntersectTypeUid { get; set; }
        [DataMember]
        public bool IsDisplayable { get; set; }
        [DataMember]
        public bool ShowIfEmpty { get; set; }
        [DataMember]
        public bool DisplayRefListDescription { get; set; }
    }

    public class FieldTypeDataTypeComputedScoreApiViewModel
    {
        [DataMember]
        public ScoreType ScoreType { get; set; }
        [DataMember]
        public bool IsDisplayable { get; set; }
        [DataMember]
        public bool IsListable { get; set; }
        [DataMember]
        public bool ShowIfEmpty { get; set; }
        [DataMember]
        public bool IsPrimaryFilter { get; set; }
        [DataMember]
        public int? ColumnOrder { get; set; }
        [DataMember]
        public int? ColumnWidth { get; set; }
        [DataMember]
        public int SortOrder { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Display Description { get; set; }
        [DataMember]
        public bool? DisplayInColumn { get; set; }
    }

    public class FieldTypeDataTypeDateApiViewModel : FieldTypeEditableApiViewModel
    {
        [DataMember]
        public DateTime? DefaultValue { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_DisplayForm Description { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Validation Validation { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Search Search { get; set; }
        [DataMember]
        public bool? DisplayInColumn { get; set; }
    }

    public class FieldTypeDataTypeDecimalApiViewModel : FieldTypeEditableApiViewModel
    {
        [DataMember]
        public decimal? DefaultValue { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_DisplayForm Description { get; set; }
        [DataMember]
        public decimal? Increment { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_ValidationDecimal Validation { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Search Search { get; set; }
        [DataMember]
        public bool? DisplayInColumn { get; set; }
    }

    public class FieldTypeDataTypeDateTimeApiViewModel : FieldTypeEditableApiViewModel
    {
        [DataMember]
        public DateTime? DefaultValue { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_DisplayForm Description { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Validation Validation { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Search Search { get; set; }
        [DataMember]
        public bool? DisplayInColumn { get; set; }
    }
    public class FieldTypeDataTypeHtmlApiViewModel : FieldTypeEditableApiViewModel
    {
        [DataMember]
        public string DefaultValue { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_DisplayForm Description { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_ValidationLength Validation { get; set; }
        [DataMember]
        public bool? DisplayInColumn { get; set; }
    }
    public class FieldTypeDataTypeJsonApiViewModel
    {
        [DataMember]
        public int? ColumnOrder { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Display Description { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Validation Validation { get; set; }
        [DataMember]
        public bool IsDisplayable { get; set; }
        [DataMember]
        public bool ShowIfEmpty { get; set; }
    }

    public class FieldTypeDataTypeJsonElementApiViewModel
    {
        [DataMember]
        public JsonAttributeApiViewModel JsonAttribute { get; set; }
        public FieldTypeDescriptionApiViewModel_Display Description { get; set; }
        [DataMember]
        public int? ColumnOrder { get; set; }
        [DataMember]
        public int? ColumnWidth { get; set; }
        [DataMember]
        public int SortOrder { get; set; }
        [DataMember]
        public bool IsDisplayable { get; set; }
        [DataMember]
        public bool IsListable { get; set; }
        [DataMember]
        public bool ShowIfEmpty { get; set; }
    }

    public class JsonAttributeApiViewModel
    {
        [DataMember]
        public string FieldName { get; set; }
        [DataMember]
        public string Path { get; set; }
        [DataMember]
        public string DataType { get; set; }
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
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Search Search { get; set; }
        [DataMember]
        public bool? DisplayInColumn { get; set; }
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
        public Guid? Uid { get; set; }
        [DataMember]
        public AssetTypeClass? Class { get; set; }
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
        public bool? AllowAllValue { get; set; }
        [DataMember]
        public string AllowAllLabel { get; set; }
        [DataMember]
        public string ParentFieldTypeName { get; set; }
        [DataMember]
        public FieldTypeDataTypeLookupApiViewModel_Filter Filter { get; set; }
        [DataMember]
        public FieldTypeDataTypeLookupApiViewModel_Format Format { get; set; }
        [DataMember]
        public FieldTypeDataTypeLookupApiViewModel_List List { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Validation Validation { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Search Search { get; set; }
        [DataMember]
        public bool? DisplayInColumn { get; set; }
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
        public FieldTypeDescriptionApiViewModel_ValidationMinMaxValue Validation { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Search Search { get; set; }
        [DataMember]
        public bool? DisplayInColumn { get; set; }
    }

    public class FieldTypeDataTypePathApiViewModel
    {
        [DataMember]
        public int? ColumnOrder { get; set; }
        [DataMember]
        public int? ColumnWidth { get; set; }
        [DataMember]
        public int SortOrder { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Display Description { get; set; }
        [DataMember]
        public bool IsDisplayable { get; set; }
        [DataMember]
        public bool IsListable { get; set; }
        [DataMember]
        public bool? DisplayInColumn { get; set; }
    }

    public class FieldTypeDataTypeRelationshipApiViewModel
    {
        [DataMember]
        public FieldTypeDescriptionApiViewModel_DisplayForm Description { get; set; }
        [DataMember]
        public Guid IntersectTypeUid { get; set; }
        [DataMember]
        public int? ColumnOrder { get; set; }
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
        public bool ShowIfEmpty { get; set; }
        [DataMember]
        public bool IsPrimaryFilter { get; set; }
        [DataMember]
        public bool? DisplayInColumn { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Search Search { get; set; }
    }

    public class FieldTypeDataTypeTextApiViewModel : FieldTypeEditableApiViewModel
    {
        [DataMember]
        public string DefaultValue { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_DisplayForm Description { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_ValidationText Validation { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Search Search { get; set; }
        [DataMember]
        public bool? DisplayInColumn { get; set; }
    }
    public class FieldTypeDataTypeTagApiViewModel
    {
        [DataMember]
        public int? ColumnOrder { get; set; }
        [DataMember]
        public int? ColumnWidth { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Display Description { get; set; }
        [DataMember]
        public int SortOrder { get; set; }
        [DataMember]
        public bool IsListable { get; set; }
        [DataMember]
        public bool IsPrimaryFilter { get; set; }
    }

    public class FieldTypeCounterApiViewModel : FieldTypeEditableApiViewModel
    {
        [DataMember]
        public FieldTypeDescriptionApiViewModel_DisplayForm Description { get; set; }
        [DataMember]
        public FieldTypeDescriptionApiViewModel_Search Search { get; set; }
        [DataMember]
        public string CounterPrefix { get; set; }
        [DataMember]
        public int? CounterInitialIndex { get; set; }
        [DataMember]
        public bool? DisplayInColumn { get; set; }
    }

    public class FieldTypeDataTypeApiViewModel
    {
        [DataMember]
        public FieldTypeDataTypeBooleanApiViewModel Boolean { get; set; }
        [DataMember]
        public FieldTypeDataTypeComputedOwnershipLookupApiViewModel ComputedOwnershipLookup { get; set; }
        [DataMember]
        public FieldTypeDataTypeComputedRelationshipFieldApiViewModel ComputedRelationshipField { get; set; }
        [DataMember]
        public FieldTypeDataTypeComputedRelationshipLookupApiViewModel ComputedRelationshipLookup { get; set; }
        [DataMember]
        public FieldTypeDataTypeComputedRelationshipReferenceListApiViewModel ComputedRelationshipReferenceList { get; set; }
        [DataMember]
        public FieldTypeCounterApiViewModel Counter { get; set; }
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
        public FieldTypeDataTypeJsonElementApiViewModel JsonElement { get; set; }
        [DataMember]
        public FieldTypeDataTypeLinkApiViewModel Link { get; set; }
        [DataMember]
        public FieldTypeDataTypeLookupApiViewModel Lookup { get; set; }
        [DataMember]
        public FieldTypeDataTypeNumberApiViewModel Number { get; set; }
        [DataMember]
        public FieldTypeDataTypePathApiViewModel Path { get; set; }
        [DataMember]
        public FieldTypeDataTypeRelationshipApiViewModel Relationship { get; set; }
        [DataMember]
        public FieldTypeDataTypeTextApiViewModel Text { get; set; }
        [DataMember]
        public FieldTypeDataTypeTagApiViewModel Tag { get; set; }
        [DataMember]
        public FieldTypeDataTypeComputedScoreApiViewModel Score { get; set; }

        public bool IsOnlyOneTypeModelDefined()
        {
            int childPopulatedCount = 0;

            childPopulatedCount += (Boolean != null) ? 1 : 0;
            childPopulatedCount += (ComputedOwnershipLookup != null) ? 1 : 0;
            childPopulatedCount += (ComputedRelationshipField != null) ? 1 : 0;
            childPopulatedCount += (ComputedRelationshipLookup != null) ? 1 : 0;
            childPopulatedCount += (ComputedRelationshipReferenceList != null) ? 1 : 0;
            childPopulatedCount += (Date != null) ? 1 : 0;
            childPopulatedCount += (DateTime != null) ? 1 : 0;
            childPopulatedCount += (Decimal != null) ? 1 : 0;
            childPopulatedCount += (Html != null) ? 1 : 0;
            childPopulatedCount += (Json != null) ? 1 : 0;
            childPopulatedCount += (JsonElement != null) ? 1 : 0;
            childPopulatedCount += (Link != null) ? 1 : 0;
            childPopulatedCount += (Lookup != null) ? 1 : 0;
            childPopulatedCount += (Number != null) ? 1 : 0;
            childPopulatedCount += (Path != null) ? 1 : 0;
            childPopulatedCount += (Relationship != null) ? 1 : 0;
            childPopulatedCount += (Text != null) ? 1 : 0;
            childPopulatedCount += (Tag != null) ? 1 : 0;
            childPopulatedCount += (Score != null) ? 1 : 0;
            childPopulatedCount += (Counter != null) ? 1 : 0;

            return (childPopulatedCount == 1);
        }

        public bool IsPartOfKey()
        {
            bool partOfKey = false;

            if (Boolean != null) partOfKey = Boolean.IsPartOfKey;
            if (Date != null) partOfKey = Date.IsPartOfKey;
            if (DateTime != null) partOfKey = DateTime.IsPartOfKey;
            if (Decimal != null) partOfKey = Decimal.IsPartOfKey;
            if (Html != null) partOfKey = Html.IsPartOfKey;
            if (Lookup != null) partOfKey = Lookup.IsPartOfKey;
            if (Number != null) partOfKey = Number.IsPartOfKey;
            if (Text != null) partOfKey = Text.IsPartOfKey;

            return partOfKey;
        }

        public string GetFieldType()
        {
            if (Boolean != null) { return "Boolean"; }
            if (ComputedOwnershipLookup != null) { return "ComputedOwnershipLookup"; }
            if (ComputedRelationshipField != null) { return "ComputedRelationshipField"; }
            if (ComputedRelationshipLookup != null) { return "ComputedRelationshipLookup"; }
            if (ComputedRelationshipReferenceList != null) { return "ComputedRelationshipReferenceList"; }
            if (Counter != null) { return "Counter"; }
            if (Date != null) { return "Date"; }
            if (DateTime != null) { return "DateTime"; }
            if (Decimal != null) { return "Decimal"; }
            if (Html != null) { return "Html"; }
            if (Json != null) { return "Json"; }
            if (JsonElement != null) { return "JsonElement"; }
            if (Link != null) { return "Link"; }
            if (Lookup != null) { return "Lookup"; }
            if (Number != null) { return "Number"; }
            if (Path != null) { return "Path"; }
            if (Relationship != null) { return "Relationship"; }
            if (Text != null) { return "Text"; }
            if (Tag != null) { return "Tag"; }
            if (Score != null) { return "Score"; }
            return "Unknown";
        }
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
        public Guid? ActionTypeUid { get; set; }
        [DataMember]
        public Guid? AssetTypeUid { get; set; }
        [DataMember]
        public Guid? RelationshipTypeUid { get; set; }
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

    public enum FieldTypesApiEditAction
    {
        Merge = 1,
        Replace = 2
    }

    public class FieldTypeApiEditModel
    {
        [DataMember]
        [Required(AllowEmptyStrings = false, ErrorMessage = "{0} is a required field")]
        [MaxLength(128, ErrorMessage = "{0} cannot exceed {1} characters.")]
        [RegularExpression("^[a-zA-Z][a-zA-Z0-9_]+$", ErrorMessage = "{0} can only have uppercase letters, lowercase letters, numbers, or underscore. It must also be greater than 1 character in length and start with a letter.")]
        public string Name { get; set; }
        [DataMember]
        [Required(AllowEmptyStrings = false, ErrorMessage = "{0} is a required field")]
        [MaxLength(250, ErrorMessage = "{0} cannot exceed {1} characters.")]
        public string FriendlyName { get; set; }
        [DataMember]
        [MaxLength(250, ErrorMessage = "{0} cannot exceed {1} characters.")]
        public string Category { get; set; }
        [DataMember]
        public FieldTypeDataTypeApiViewModel Type { get; set; }
    }

    public class FieldTypesApiEditModel : BaseFieldTypesApiModel
    {
        [DataMember]
        public FieldTypesApiEditAction Action { get; set; }

        [DataMember]
        public List<FieldTypeApiEditModel> Fields { get; set; }
    }

    public class FieldTypeApiDeleteModel
    {
        [DataMember]
        public string Name { get; set; }
    }

    public class FieldTypesApiDeleteModel : BaseFieldTypesApiModel
    {
        [DataMember]
        public List<FieldTypeApiDeleteModel> Fields { get; set; }

    }

    public class BaseFieldTypesApiModel
    {
        [DataMember]
        public Guid? ActionTypeUid { get; set; } = null;

        [DataMember]
        public Guid? AssetTypeUid { get; set; } = null;

        [DataMember]
        public Guid? RelationshipTypeUid { get; set; } = null;
    }

    public class MoveModel
    {
        [DataMember]
        public Guid? TypeUid { get; set; }

        [DataMember]
        public string FieldTypename { get; set; }

        [DataMember]
        public string Direction { get; set; }
    }

    public class FieldTypeCore : BaseIntObject, IIntObject
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string FriendlyName { get; set; }

        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public bool AllowMultipleValues { get; set; }

        [DataMember]
        public decimal? MinimumLength { get; set; }

        [DataMember]
        public decimal? MaximumLength { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(1000)]
        public string Pattern { get; set; }

        [DataMember]
        public int? Length { get; set; }

        [DataMember]
        public bool HasDefaultValue { get; set; }

        [DataMember]
        public bool IsRequired { get; set; }

        [DataMember]
        public bool IsPartOfKey { get; set; }
    }
}
