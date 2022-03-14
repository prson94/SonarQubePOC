using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class CompanyDomainGroup : BaseIntObject
    {
        [DataMember]
        public int CompanyID { get; set; }

        [DataMember]
        public int DomainSettingID { get; set; }

        [DataMember]
        public string GroupName { get; set; }

        [DataMember]
        public bool IsAdministrator { get; set; }
    }
}
