using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FusionAttributeTypePromotion : BaseObject
    {
        [Key, Column(Order = 1), DataMember]
        public int FusionAttributeTypeID { get; set; }

        [Key, Column(Order = 2)]
        public string ObjectType { get; set; }

        [Key, Column(Order = 3)]
        public int ObjectID { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Parent_Name", Description = "Parent_Description")]
        public int? ParentID { get; set; }

        [DataMember]
        public int? TaxonomyTypeID { get; set; }
    }
}
