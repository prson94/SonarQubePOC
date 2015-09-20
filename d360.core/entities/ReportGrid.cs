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
    [DataContract(Namespace = NAMESPACE)]
    public class ReportGrid : BaseCompanyObject, ICompanyObject, IIntObject
    {
        [DataMember]
        public string Columns { get; set; }

        [DataMember]
        public string Fields { get; set; }

        [DataMember]
        public string Location { get; set; }
        
        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Name_ErrorRequired")]
        [StringLength(250)]
        public string Name { get; set; }

        [DataMember]
        public int ReportID { get; set; }

        [DataMember]
        public string SQL { get; set; }

        public virtual Report Report { get; set; }
    }
}
