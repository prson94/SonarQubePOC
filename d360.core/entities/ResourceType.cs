using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(ObjectTypeInfo.ResourceType, "ResourceType")]
    public class ResourceType : BaseIntObject, IIntObject
    {
        #region Properties

        [DataMember, Key, ReadOnly(true)]
        public new int ID { get; set; }

        [DataMember]
        public DateTime DateCreated { get; set; }

        [DataMember]
        public DateTime DateUpdated { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Name_ErrorRequired")]
        [StringLength(250)]
        public string Name { get; set; }

        #endregion

        [IgnoreDataMember, ForeignKey("ResourceTypeID")]
        public virtual ICollection<Resource> Resources { get; set; }
    }
}
