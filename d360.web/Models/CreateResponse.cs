using d360.core;
using System.Runtime.Serialization;

namespace d360.web.Models
{
    [DataContract(Namespace = constants.NAMESPACE)]
    public class CreateResponse
    {
        [DataMember]
        public string Message { get; set; }
    }
}