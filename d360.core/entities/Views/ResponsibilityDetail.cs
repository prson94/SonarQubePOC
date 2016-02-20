using System.Runtime.Serialization;

namespace d360.core.entities.Views
{
    [DataContract(Namespace = NAMESPACE)]
    public class ResponsibilityDetail : ResponsibilityDetailBase
    {
        [DataMember]
        public bool Visible { get; set; }
        
        [DataMember]
        public int? PrimaryOwnerResourceID { get; set; }

        [DataMember]
        public string PrimaryOwnerResourceName { get; set; }

        [DataMember]
        public string PrimaryOwnerResourceUrl { get; set; }

        [DataMember]
        public int ObjectTypeID { get; set; }
    }
}
