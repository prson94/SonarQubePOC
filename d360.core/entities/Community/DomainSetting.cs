using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class DomainSetting : BaseIntObject, IIntObject
    {
        [DataMember]
        public string IdpSsoEndpoint { get; set; }

        [DataMember]
        public string IdpSloEndpoint { get; set; }

        public int? IdpDomainCertificateID { get; set; }

        public int? SpDomainCertificateID { get; set; }

        public HashAlgorithmType HashAlgorithmType { get; set; }

        public bool SignInitialSSORequest { get; set; }

        public string AuthenticationSettings { get; set; }

        [IgnoreDataMember, ForeignKey("IdpDomainCertificateID")]
        public DomainCertificate IdpDomainCertificate { get; set; }

        [IgnoreDataMember, ForeignKey("SpDomainCertificateID")]
        public DomainCertificate SpDomainCertificate { get; set; }

        [IgnoreDataMember, ForeignKey("DomainSettingID")]
        public virtual ICollection<CompanyDomainSetting> CompanyDomainSettings { get; set; }
    }
}
