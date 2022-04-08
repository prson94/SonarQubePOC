using System.Runtime.Serialization;

namespace d360.core.entities
{
    public class AssetTagList
    {
        [DataMember]
        public string DisplayName { get; set; }

        [DataMember]
        public string Breadcrumbs { get; set; }

        [DataMember]
        public string Url { get; set; }
    }
}
