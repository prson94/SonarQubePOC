using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class DomainCertificate : BaseIntObject, IIntObject
    {
        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        public string Name { get; set; }

        public byte[] File { get; set; }

        [DataMember]
        public string Password { get; set; }

    }
}
