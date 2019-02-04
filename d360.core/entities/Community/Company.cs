using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.entities.Contracts;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Company : BaseIntObject, IIntObject
    {
        [DataMember]
        public int DatabaseServerID { get; set; }

        [DataMember]
        public bool SynchAgentLog { get; set; }

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
        public virtual ICollection<Plugins.Package> Packages { get; set; }

        [IgnoreDataMember, ForeignKey("CompanyID")]
        public virtual ICollection<CompanyDomainSetting> CompanyDomainSettings { get; set; }

        [IgnoreDataMember, ForeignKey("CompanyID")]
        public virtual ICollection<CompanyFeature> CompanyFeatures { get; set; }

        [IgnoreDataMember, ForeignKey("CompanyID")]
        public virtual ICollection<CompanySetting> CompanySettings { get; set; }
    }
}
