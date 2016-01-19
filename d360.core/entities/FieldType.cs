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
        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        public string Name { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "DisplayDescription_Name", Description = "DisplayDescription_Description")]
        public string DisplayDescription { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "FormDescription_Name", Description = "FormDescription_Description")]
        public string FormDescription { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "ValidationDescription_Name", Description = "ValidationDescription_Description")]
        public string ValidationDescription { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "FriendlyName_Name", Description = "FriendlyName_Description")]
        public string FriendlyName { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Type_Name", Description = "Type_Description")]
        public string Type { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "LookupObjectType_Name", Description = "LookupObjectType_Description")]
        public string LookupObjectType { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "LookupObjectID_Name", Description = "LookupObjectID_Description")]
        public int? LookupObjectID { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "LookupDisplayFormat_Name", Description = "LookupDisplayFormat_Description")]
        public string LookupDisplayFormat { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Length_Name", Description = "Length_Description")]
        public int? Length { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "MinimumLength_Name", Description = "MinimumLength_Description")]
        public int? MinimumLength { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "MaximumLength_Name", Description = "MaximumLength_Description")]
        public int? MaximumLength { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Pattern_Name", Description = "Pattern_Description")]
        public string Pattern { get; set; }

        [DataMember]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "IsListable_Name", Description = "IsListable_Description")]
        public bool IsListable { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "IsRequired_Name", Description = "IsRequired_Description")]
        public bool IsRequired { get; set; }

        [DataMember]
        public int SortOrder { get; set; }

        [IgnoreDataMember, ForeignKey("FieldTypeID")]
        public virtual ICollection<Field> Fields { get; set; }

        [IgnoreDataMember, ForeignKey("FieldTypeID")]
        public virtual ICollection<FieldTypeFusionLookupDefinition> FieldTypeFusionLookupDefinitions { get; set; }
    }
}
