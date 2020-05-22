using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
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

        [DataMember]
        public bool Locked { get; set; }

        [DataMember]
        public SettingType SettingType { get; set; }

        [IgnoreDataMember, ForeignKey("SettingID")]
        public virtual ICollection<CompanySetting> CompanySettings { get; set; }
    }

    public class IPs
    {
        public List<Ip> Ip { get; set; }
    }
    public class Ip
    {
        public string Name { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
    }
}
