using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
}
