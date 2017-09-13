using d360.core.entities.Contracts;
using System;
using System.Data;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class LookupType : BaseIntObject, IIntObject, ISearchable, IUpdatedMetadata
    {
        [DataMember(Name = "name")]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Name_ErrorRequired")]
        [StringLength(250)]
        public string Name { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        [XmlIgnore()]
        [ForeignKey("LookupTypeID")]
        public virtual ICollection<Lookup> Lookups { get; set; }
    }
}
