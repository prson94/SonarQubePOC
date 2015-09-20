using System.Xml.Linq;
using d360.core.entities.Contracts;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ResponsibilityTransformation : BaseIntObject, IIntObject, IUpdatedMetadata
    {
        [DataMember]
        public int ResponsibilityID { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "ResponsibilityTransformationType_Name", Description = "ResponsibilityTransformationType_Description")]
        public ResponsibilityTransformationType ResponsibilityTransformationType { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "ResponsibilityTransformation_Description_Description")]
        public string Description { get; set; }

        [DataMember]
        public DateTime? UpdatedOn { get; set; }

        [DataMember]
        public int? UpdatedBy { get; set; }

        [IgnoreDataMember]
        public virtual Responsibility Responsibility { get; set; }
    }
}
