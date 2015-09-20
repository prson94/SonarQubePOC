using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Role : BaseObject, IIntObject
    {
        [
        DataMember,
        Key,
        DatabaseGenerated(DatabaseGeneratedOption.Identity),
        Display(ResourceType = typeof(d360.core.resources.Fields), Name = "ID_Name", Description = "ID_Description")
        ]
        public int ID { get; set; }

        [
        DataMember,
        Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description"),
        Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Name_ErrorRequired"),
        StringLength(250)
        ]
        public string Name { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "IsGlobal_Name", Description = "IsGlobal_Description")]
        public bool IsGlobal { get; set; }
    }
}
