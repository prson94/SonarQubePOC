using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using d360.core.entities;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Serialization;
using System.Web.Script.Serialization;
using System.ComponentModel;
using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class DomainCertificate : BaseIntObject, IIntObject
    {
        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        public string Name { get; set; }

        public byte[] File { get; set; }

        [DataMember]
        public string Password { get; set; }

    }
}
