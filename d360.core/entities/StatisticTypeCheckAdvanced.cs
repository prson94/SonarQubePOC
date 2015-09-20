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
    public class StatisticTypeCheckAdvanced : BaseObject, IStatisticTypeCheck, ICompanyObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int CompanyID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int StatisticTypeID { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Check_SQL_Name", Description = "Check_SQL_Description")]
        public string SQL { get; set; }

        //[XmlIgnore()]
        //[ForeignKey("CompanyID, StatisticTypeID")]
        //public virtual StatisticType StatisticType { get; set; }
    }
}
