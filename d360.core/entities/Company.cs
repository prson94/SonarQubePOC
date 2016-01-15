using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Company : BaseIntObject, IIntObject
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int DatabaseServerID { get; set; }

        [DataMember]
        public string Status { get; set; }

        [DataMember]
        public bool SynchAgentLog { get; set; }

        [IgnoreDataMember]
        public virtual DatabaseServer DatabaseServer { get; set; }

        [IgnoreDataMember, ForeignKey("CompanyID")]
        public virtual ICollection<Plugins.Package> Packages { get; set; }

        [IgnoreDataMember, ForeignKey("CompanyID")]
        public virtual ICollection<CompanyDomainSetting> CompanyDomainSettings { get; set; }

        [IgnoreDataMember, ForeignKey("CompanyID")]
        public virtual ICollection<CompanySetting> CompanySettings { get; set; }
    }
}
