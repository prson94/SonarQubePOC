using System.Collections.Generic;
using System.Xml.Linq;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Web.Script.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(ObjectTypeInfo.Vocabulary, "Vocabulary")]
    public class Vocabulary : BaseIntObject, IIntObject, ICreatedObject, IUpdatedObject, IUpdatedMetadata
    {
        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        public string Name { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
