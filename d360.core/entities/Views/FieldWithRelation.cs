using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    public class FieldWithRelation : BaseObject
    {
        public long ID { get; set; }

        [Key, Column(Order = 1)]
        public int FieldTypeID { get; set; }
        
        public string Name { get; set; }
        
        public string FriendlyName { get; set; }
        
        public string Category { get; set; }

        [Display(ResourceType = typeof(resources.Fields), Name = "DisplayDescription_Name", Description = "DisplayDescription_Description")]
        public string DisplayDescription { get; set; }

        [Display(ResourceType = typeof(resources.Fields), Name = "FormDescription_Name", Description = "FormDescription_Description")]
        public string FormDescription { get; set; }

        [DataMember, Display(ResourceType = typeof(resources.Fields), Name = "ValidationDescription_Name", Description = "ValidationDescription_Description")]
        public string ValidationDescription { get; set; }

        [Column(TypeName = "varchar"), StringLength(25)]
        public string Type { get; set; }

        [Column(TypeName = "varchar"), StringLength(25)]
        public string LookupObjectType { get; set; }
        
        public int? LookupObjectID { get; set; }
        
        public string LookupDisplayFormat { get; set; }
        
        public int? Length { get; set; }
        
        public decimal? MaximumLength { get; set; }
        
        public decimal? MinimumLength { get; set; }
        
        public string Pattern { get; set; }

        /* FieldTypeRelation Properties */
        public bool IsDisplayable { get; set; }
        
        public bool IsEditable { get; set; }
        
        public bool IsListable { get; set; }
        
        public bool IsRequired { get; set; }
        
        public int SortOrder { get; set; }
        
        public bool AllowMultipleValues { get; set; }

        /* Field Properties */
        public string ObjectType { get; set; }
        
        public int? ObjectID { get; set; }

		public int? AssetTypeID { get; set; }
		public long? AssetID { get; set; }

		public int? IntersectTypeID { get; set; }
		public int? IntersectID { get; set; }

		public int? IssueTypeID { get; set; }
		public int? IssueID { get; set; }

		public string Value { get; set; }
        
        public string FormattedValue { get; set; }
        
        [Column(TypeName = "varchar"), StringLength(500)]
        public string LookupUrl { get; set; }
    }
}
