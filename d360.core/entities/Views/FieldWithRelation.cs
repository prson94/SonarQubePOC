using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    public class FieldWithRelation : BaseObject
    {
        [Key, Column(Order = 1)]
        public int FieldTypeID { get; set; }
        public string Name { get; set; }
        public string FriendlyName { get; set; }
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "DisplayDescription_Name", Description = "DisplayDescription_Description")]
        public string DisplayDescription { get; set; }

        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "FormDescription_Name", Description = "FormDescription_Description")]
        public string FormDescription { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "ValidationDescription_Name", Description = "ValidationDescription_Description")]
        public string ValidationDescription { get; set; }

        public string Type { get; set; }
        public string LookupObjectType { get; set; }
        public int? LookupObjectID { get; set; }
        public string LookupDisplayFormat { get; set; }
        public int? Length { get; set; }
        public int? MaximumLength { get; set; }
        public int? MinimumLength { get; set; }
        public string Pattern { get; set; }

        /* FieldTypeRelation Properties */
        public bool IsListable { get; set; }
        public bool IsRequired { get; set; }
        public int SortOrder { get; set; }

        /* Field Properties */
        [Key, Column(Order = 2)]
        public string ObjectType { get; set; }
        [Key, Column(Order = 3)]
        public int ObjectID { get; set; }
        public string Value { get; set; }
        public string FormattedValue { get; set; }
        public string LookupUrl { get; set; }
    }
}
