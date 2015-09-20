using d360.core;
using System.Runtime.Serialization;

namespace d360.web.Models
{
    [DataContract(Namespace = constants.NAMESPACE)]
    public class PageActionItemData
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Value { get; set; }
    }
}