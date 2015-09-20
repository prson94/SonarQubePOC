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

namespace d360.core.entities.Views
{
    [DataContract(Namespace = NAMESPACE)]
    public class ResponsibilityDetail : ResponsibilityDetailBase
    {
        [DataMember]
        public bool Visible { get; set; }
        
        [DataMember]
        public int? PrimaryOwnerResourceID { get; set; }

        [DataMember]
        public string PrimaryOwnerResourceName { get; set; }

        [DataMember]
        public string PrimaryOwnerResourceUrl { get; set; }

        [DataMember]
        public bool RedFlagged { get; set; }

        [DataMember]
        public int ObjectTypeID { get; set; }
    }
}
