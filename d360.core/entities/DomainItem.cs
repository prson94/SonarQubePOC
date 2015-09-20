using System.Collections.Generic;
using System.Xml.Linq;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(ObjectTypeInfo.DomainItem, "DomainItem")]
    public class DomainItem : BaseIntObject, IIntObject, ISearchable, IUpdatedMetadata
    {
        #region Properties

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Code_Name", Description = "Code_Description"), StringLength(50)]
        public string Code { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "Description_Description")]
        public string Description { get; set; }

        [DataMember]
        public int DomainID { get; set; }

        [
        DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description"),
        Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Name_ErrorRequired"), StringLength(250)
        ]
        public string Name { get; set; }

        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Parent_Name", Description = "Parent_Description")]
        public string Parents { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        #endregion

        #region Navigation Properties

        [IgnoreDataMember]
        public virtual Domain Domain { get; set; }

        [IgnoreDataMember]
        public virtual ICollection<IntersectFlowMapping> Mappings { get; set; }

        #endregion
    }
}
