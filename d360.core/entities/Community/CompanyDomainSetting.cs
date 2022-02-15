using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class CompanyDomainSetting : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int CompanyID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int DomainSettingID { get; set; }

        [DataMember]
        public AuthenticationType AuthenticationType { get; set; }

        public bool AllowNewUserLogin { get; set; }

        public string UrlPrefix { get; set; }

        public bool IsPrimary { get; set; }

        [IgnoreDataMember, ForeignKey("CompanyID")]
        public Company Company { get; set; }

        [IgnoreDataMember, ForeignKey("DomainSettingID")]
        public DomainSetting DomainSetting { get; set; }
    }
}
