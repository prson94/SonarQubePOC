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
    public class SourcingResponsibilityDetail : ResponsibilityDetailBase
    {
        [DataMember]
        public string ResponsibleObjectIconBackColor { get; set; }

        [DataMember]
        public string ResponsibleObjectIconForeColor { get; set; }

        [DataMember]
        public string ResponsibleObjectIconText { get; set; }


        [DataMember]
        public bool Actual { get; set; }
    }
}
