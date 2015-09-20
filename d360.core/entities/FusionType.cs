using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Name = NAMESPACE), ObjectType(ObjectTypeInfo.FusionType, "FusionType")]
    public class FusionType : BaseIntObject, IIntObject, ISearchable, IUpdatedMetadata
    {
        [
        DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description"), 
        Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Name_ErrorRequired"), StringLength(250)
        ]
        public string Name { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "Description_Description")]
        public string Description { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        [IgnoreDataMember, ForeignKey("FusionTypeID")]
        public virtual ICollection<Fusion> Fusions { get; set; }

        [IgnoreDataMember, ForeignKey("FusionTypeID")]
        public virtual ICollection<FusionAttributeType> FusionAttributeTypes { get; set; }
    }
}
