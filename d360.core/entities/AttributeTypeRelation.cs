using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public partial class AttributeTypeRelation : BaseObject
    {
        [Column(Order = 1), DataMember, Key]
        public int AttributeTypeID { get; set; }

        [Column(Order = 2), DataMember, Key]
        public string ObjectType { get; set; }

        [Column(Order = 3), DataMember, Key]
        public int ObjectID { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "AllowMultipleEntries_Name", Description = "AllowMultipleEntries_Description")]
        public bool AllowMultipleEntries { get; set; }

        public virtual AttributeType AttributeType { get; set; }
    }
}
