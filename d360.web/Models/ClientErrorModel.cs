using System.Runtime.Serialization;

namespace d360.web.Models
{
    public class ClientErrorModel
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public string Stack { get; set; }
    }
}
