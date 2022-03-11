using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FieldTypeWithRelation : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int ID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Category { get; set; }

        [DataMember]
        public string FriendlyName { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(resources.Fields), Name = "DisplayDescription_Name", Description = "DisplayDescription_Description")]
        public string DisplayDescription { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(resources.Fields), Name = "FormDescription_Name", Description = "FormDescription_Description")]
        public string FormDescription { get; set; }

        [DataMember, Display(ResourceType = typeof(resources.Fields), Name = "ValidationDescription_Name", Description = "ValidationDescription_Description")]
        public string ValidationDescription { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(25)]
        public string Type { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(25)]
        public string LookupObjectType { get; set; }

        [DataMember]
        public int? LookupObjectID { get; set; }

        [DataMember]
        public string LookupDisplayFormat { get; set; }

        [DataMember]
        public int? Length { get; set; }

        [DataMember]
        public int? MinimumLength { get; set; }

        [DataMember]
        public int? MaximumLength { get; set; }

        [DataMember]
        public string Pattern { get; set; }

        [DataMember, Key, Column(Order = 2, TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public int ObjectID { get; set; }

        [DataMember]
        public string ObjectName { get; set; }

        [DataMember]
        public bool IsDisplayable { get; set; }

        [DataMember]
        public bool IsEditable { get; set; }

        [DataMember]
        public bool IsListable { get; set; }

        [DataMember]
        public bool IsRequired { get; set; }

        [DataMember]
        public int SortOrder { get; set; }
    }
}
