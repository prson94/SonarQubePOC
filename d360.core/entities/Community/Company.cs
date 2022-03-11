using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Company : BaseIntObject, IIntObject
    {

        [DataMember]
        public string Status { get; set; }

        [DataMember]
        public int DatabaseServerID { get; set; }

        [DataMember]
        public int ClientID { get; set; }

        [DataMember]
        public EnvironmentLevel EnvironmentLevel { get; set; }

        [DataMember]
        public string Notes { get; set; }

        [DataMember]
        public int Priority { get; set; }

        [IgnoreDataMember]
        public virtual DatabaseServer DatabaseServer { get; set; }

        [IgnoreDataMember, ForeignKey("CompanyID")]
        public virtual ICollection<CompanyDomainSetting> CompanyDomainSettings { get; set; }
    }
}
