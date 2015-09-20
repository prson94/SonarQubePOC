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
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Setting : BaseIntObject, IIntObject
    {
        [DataMember]
        public string FieldName { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string DefaultValue { get; set; }

        [IgnoreDataMember, ForeignKey("SettingID")]
        public virtual ICollection<CompanySetting> CompanySettings { get; set; }
    }
}
