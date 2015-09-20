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
    public class StatisticTypeCheckProperty : BaseObject, IStatisticTypeCheck, ICompanyObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int CompanyID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int StatisticTypeID { get; set; }

        [DataMember]
        public string ObjectType { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Check_PropertyName_Name", Description = "Check_PropertyName_Description")]
        public string PropertyName { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Check_PropertyValue_Name", Description = "Check_PropertyValue_Description")]
        public string Value { get; set; }

        //[XmlIgnore()]
        //[ForeignKey("CompanyID, StatisticTypeID")]
        //public virtual StatisticType StatisticType { get; set; }
    }
}
