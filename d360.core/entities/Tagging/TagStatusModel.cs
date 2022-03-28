using System.Runtime.Serialization;

namespace d360.core.entities
{
    public class TagStatusModel
    {
        [DataMember]
        public bool IsTaggingEnabled { get; set; }
    }
}
