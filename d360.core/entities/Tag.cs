using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Xml.Linq;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Tag : BaseIntObject, IIntObject, IUpdatedMetadata
    {
        #region Properties

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        public string Name { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        #endregion

        #region Collection Properties

        [ForeignKey("TagID"), IgnoreDataMember]
        public virtual ICollection<TagRelation> Relations { get; set; }

        #endregion
    }
}
