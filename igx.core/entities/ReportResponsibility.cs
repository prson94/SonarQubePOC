using System;
using System.Collections.Generic;
using System.Linq;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ReportResponsibility : BaseIntObject, IIntObject
    {        
        [DataMember]
        public int ReportID { get; set; }

        [DataMember]
        public int ResponsibilityTypeID { get; set; }        
    }
}